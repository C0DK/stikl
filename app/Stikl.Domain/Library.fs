module domain

open System
open System.Collections.Concurrent
open System.Threading
open System.Threading.Channels
open System.Threading.Tasks
open Microsoft.FSharp.Reflection
open FSharp.Control

type PlantId =
    | PlantId of string

    member this.value =
        match this with
        | PlantId value -> value

    override this.ToString() = this.value

module PlantId =
    let isSafeChar c =
        Char.IsLetterOrDigit(c) || [| '-'; '_'; '.' |] |> Array.contains c

    let isValid v =
        v |> String.forall isSafeChar && not (String.IsNullOrWhiteSpace v)

    // TODO fail if invalid
    let parse v = PlantId v

type Plant =
    { id: PlantId
      name: string
      image_url: string }

type PlantRepository =
    { getAll: unit -> Plant List Task
      get: PlantId -> Plant Option Task
      exists: PlantId -> bool Task }

// TODO: validate Username, to not include spaces etc - be URL safe.
type Username =
    | Username of string

    member this.value =
        match this with
        | Username value -> value

    override this.ToString() = this.value

module Username =
    // These
    let random = Guid.NewGuid().ToString() |> Username

    let isSafeChar c =
        Char.IsLetterOrDigit(c) || [| '-'; '_'; '.' |] |> Array.contains c

    let isValid v =
        v |> String.forall isSafeChar && not (String.IsNullOrWhiteSpace v)

type SeedKind =
    | Seed
    | Seedling
    | Cutting
    | WholePlant

type PlantOffer =
    { plant: Plant
      comment: string option
      seedKind: SeedKind }

type Location =
    { label: string
      lat: decimal
      lon: decimal }

type DawaLocation = { id: Guid; location: Location }

type CreateUserPayload =
    { username: Username
      firstName: string
      lastName: string
      authId: string
      location: DawaLocation }
// TODO: is it best / bad to include the whole plant in the event??
type UserEventPayload =
    | CreateUser of CreateUserPayload
    | AddedWant of Plant
    | AddedSeeds of PlantOffer
    | RemovedWant of Plant
    | RemovedSeeds of Plant
    | UpdateName of firstName: string * lastName: string
    | SetDawaLocation of DawaLocation
    //| AggregateEvent of UserEventPayload list
    | MessageSent of message: string * receiver: Username
    | MessageReceived of message: string * sender: Username

    member this.kind =
        match FSharpValue.GetUnionFields(this, typeof<UserEventPayload>) with
        | case, _ -> case.Name


type UserEvent =
    { user: Username
      payload: UserEventPayload
      timestamp: DateTimeOffset }

module UserEvent =
    let create payload username =
        { user = username
          payload = payload
          timestamp = DateTimeOffset.UtcNow }

module Chat =
    type Message =
        | MessageSent of string
        | MessageReceived of string

type User =
    { username: Username
      authId: string
      imgUrl: string
      firstName: string
      lastName: string
      wants: Plant Set
      location: DawaLocation
      seeds: PlantOffer Set
      // TODO: add timestamp to user event here - i.e `(DateTimeOffset * UserEvent)`
      chats: Map<Username, (DateTimeOffset * Chat.Message) list>
      history: UserEvent list }


    member this.fullName = $"{this.firstName} {this.lastName}"



type UserStore =
    abstract member Get: username: Username -> cancellationToken: CancellationToken -> User option Task
    abstract member GetByAuthId: authId: string -> cancellationToken: CancellationToken -> User option Task
    abstract member GetAll: cancellationToken: CancellationToken -> User list Task
    abstract member Query: string -> cancellationToken: CancellationToken -> User list Task

    abstract member ApplyEvent:
        event: UserEvent -> cancellationToken: CancellationToken -> Result<UserEvent, string> Task

    abstract member ApplyEvents:
        event: UserEvent list -> cancellationToken: CancellationToken -> Result<UserEvent list, string> Task

type CurrentUser =
    | AuthedUser of User
    | NewUser of authId: string
    | Anonymous

    member this.get =
        match this with
        | AuthedUser user -> Some user
        | NewUser _ -> None
        | Anonymous -> None


type EventBroker() =
    let channels: ConcurrentDictionary<Guid, UserEvent Channel> =
        ConcurrentDictionary<Guid, UserEvent Channel>()

    member this.Listen(cancellationToken: CancellationToken) : UserEvent TaskSeq =
        let channel = Channel.CreateUnbounded<UserEvent>()

        let id = Guid.NewGuid()

        while (channels.TryAdd(id, channel)) do
            ()

        channel.Reader.ReadAllAsync cancellationToken

    member this.Publish (event: UserEvent) (cancellationToken: CancellationToken) : Task =
        channels.Values
        |> Seq.map _.Writer.WriteAsync(event, cancellationToken)
        |> ValueTask.whenAll
type EventHandler(users: UserStore, eventBroker: EventBroker, identity: CurrentUser) =
       member this.handleMultiple (events: UserEvent list) (cancellationToken: CancellationToken)= 
            users.ApplyEvents events cancellationToken
            |> Task.collect (
                Result.map (fun es ->
                    task {
                        do! es |> Seq.map (fun e -> eventBroker.Publish e cancellationToken) |> Task.whenAll
                        
                        return es
                    })
                >> Task.unpackResult
            )
    
        
        member this.handle (payload: UserEventPayload) (cancellationToken: CancellationToken): Result<UserEvent, string> Task =
            let username = 
                match identity with
                | AuthedUser user ->
                    match payload with
                    | CreateUser _ -> Error "You cannot create user twice"
                    | _ -> Ok user.username
                | Anonymous -> Error "Cannot do things if you aren't logged in"
                | NewUser _ ->
                    match payload with
                    | CreateUser createUser -> Ok createUser.username 
                    | _ -> Error "You cannot do that until your user is created"
                    
            match username with
            | Ok username -> this.handleMultiple [(UserEvent.create payload username)] cancellationToken |> Task.map (Result.map List.head)
            | Error e -> Task.FromResult(Error e)
        
module User =
    let Wants (id: PlantId) user =
        user.wants |> Set.exists (fun p -> p.id = id)

    let Has id user =
        user.seeds |> Set.exists (fun p -> p.plant.id = id)

    let GetWants user = user.wants

    let GetSeeds user = user.seeds

let rec private applyWithoutHistory (event: UserEvent) (user: User) =
    let Without plant = Set.filter (fun p -> p.plant <> plant)

    (match event.payload with
     //| AggregateEvent events -> events |> List.fold (fun user event -> applyWithoutHistory event user) user
     | AddedWant plant ->
         { user with
             wants = Set.add plant user.wants }
     | AddedSeeds plantOffer ->
         { user with
             seeds = user.seeds |> Without plantOffer.plant |> Set.add plantOffer }
     | RemovedWant plant ->
         { user with
             wants = Set.remove plant user.wants }
     | RemovedSeeds plant ->
         { user with
             seeds = user.seeds |> Without plant }
     | UpdateName(firstName, lastName) ->
         { user with
             firstName = firstName
             lastName = lastName }
     | CreateUser payload -> failwith "Cannot apply CreateUser to existing user"
     | SetDawaLocation dawaLocation -> { user with location = dawaLocation }
     | MessageSent(message, receiver) ->
         let existingChat = user.chats |> Map.tryFind receiver |> Option.defaultValue []

         let updatedChats =
             Map.add receiver ((event.timestamp, Chat.MessageSent message) :: existingChat) user.chats

         { user with chats = updatedChats }
     | MessageReceived(message, sender) ->
         let existingChat = user.chats |> Map.tryFind sender |> Option.defaultValue []

         let updatedChats =
             Map.add sender ((event.timestamp, Chat.MessageReceived message) :: existingChat) user.chats

         { user with chats = updatedChats })

// TODO an apply with an optional user?
let apply (event: UserEvent) (user: User) =
    { applyWithoutHistory event user with
        history = event :: user.history }

let applyOnOptional (event: UserEvent) (user: User option) =
    match user with
    | Some user ->
        { applyWithoutHistory event user with
            history = event :: user.history }
    | None ->
        match event.payload with
        | CreateUser payload ->
            { username = payload.username
              imgUrl = $"https://api.dicebear.com/9.x/shapes/svg?seed={payload.username.value}"
              authId = payload.authId
              firstName = payload.firstName
              lastName = payload.lastName
              wants = Set.empty
              location = payload.location
              seeds = Set.empty
              chats = Map.empty
              history = List.singleton event }
        | payload -> failwith $"Cannot apply '{payload.kind}' on none-existing user"

type ToastVariant =
    | SuccessToast
    | ErrorToast

type Toast =
    { variant: ToastVariant
      title: string
      message: string }
