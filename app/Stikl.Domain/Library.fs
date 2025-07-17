module domain

open System
open System.Threading.Tasks

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

type CreateUserPayload =
    { username: Username
      firstName: string
      lastName: string
      authId: string }
// TODO: is it best / bad to include the whole plant in the event??
// TODO: add an actual eventstore of (UserId * Event)
// TODO: consider how we handle events with two users - i.e SendMessage
type UserEventPayload =
    // TODO: handle create user??
    // TODO: require firstname etc
    | CreateUser of CreateUserPayload
    | AddedWant of Plant
    | AddedSeeds of PlantOffer
    | RemovedWant of Plant
    | RemovedSeeds of Plant
    | UpdateName of firstName: string * lastName: string

type UserEvent =
    { user: Username
      payload: UserEventPayload
      timestamp: DateTimeOffset }

module UserEvent =
    let create payload username =
        { user = username
          payload = payload
          timestamp = DateTimeOffset.UtcNow }

type User =
    { username: Username
      authId: string
      imgUrl: string
      firstName: string
      lastName: string
      wants: Plant Set
      seeds: PlantOffer Set
      // TODO: add timestamp to user event here - i.e `(DateTimeOffset * UserEvent)`
      history: UserEventPayload list }

    member this.fullName = $"{this.firstName} {this.lastName}"


type EventHandler =
    { handle: UserEventPayload -> Result<UserEvent, string> Task }

type UserStore =
    abstract member Get: username: Username -> User option Task
    abstract member GetByAuthId: authId: string -> User option Task
    abstract member GetAll: unit -> User list Task
    abstract member Query: string -> User list Task
    abstract member ApplyEvent: event: UserEvent -> Result<UserEvent, string> Task




module User =
    let Wants (id: PlantId) user =
        user.wants |> Set.exists (fun p -> p.id = id)

    let Has id user =
        user.seeds |> Set.exists (fun p -> p.plant.id = id)

    let GetWants user = user.wants

    let GetSeeds user = user.seeds

    let createFull (authId: string) (username: Username) (firstName: string) (lastName: string) =
        { username = username
          imgUrl = $"https://api.dicebear.com/9.x/shapes/svg?seed={username.value}"
          // TODO: ability to set
          authId = authId
          firstName = firstName
          lastName = lastName
          wants = Set.empty
          seeds = Set.empty
          history = List.empty }

let apply (event: UserEventPayload) (user: User) =
    let Without plant = Set.filter (fun p -> p.plant <> plant)

    let user =
        (match event with
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
         | CreateUser payload ->
             if user.history |> Seq.isEmpty then
                 { user with
                     username = payload.username
                     firstName = payload.firstName
                     lastName = payload.lastName
                     authId = payload.authId
                     imgUrl = $"https://api.dicebear.com/9.x/shapes/svg?seed={payload.username.value}" }
             else
                 failwith "Cannot apply CreateUser to existing user")

    { user with
        history = event :: user.history }
