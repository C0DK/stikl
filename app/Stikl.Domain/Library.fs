module domain

open System
open System.Threading
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
    | AggregateEvent of UserEventPayload list

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
      location: DawaLocation
      seeds: PlantOffer Set
      // TODO: add timestamp to user event here - i.e `(DateTimeOffset * UserEvent)`
      history: UserEventPayload list }

    member this.fullName = $"{this.firstName} {this.lastName}"


type EventHandler =
    { handle: UserEventPayload -> CancellationToken -> Result<UserEvent, string> Task }

type UserStore =
    abstract member Get: username: Username -> cancellationToken: CancellationToken -> User option Task
    abstract member GetByAuthId: authId: string -> cancellationToken: CancellationToken -> User option Task
    abstract member GetAll: cancellationToken: CancellationToken -> User list Task
    abstract member Query: string -> cancellationToken: CancellationToken -> User list Task

    abstract member ApplyEvent:
        event: UserEvent -> cancellationToken: CancellationToken -> Result<UserEvent, string> Task




module User =
    let Wants (id: PlantId) user =
        user.wants |> Set.exists (fun p -> p.id = id)

    let Has id user =
        user.seeds |> Set.exists (fun p -> p.plant.id = id)

    let GetWants user = user.wants

    let GetSeeds user = user.seeds

    let create (payload: CreateUserPayload) =
        { username = payload.username
          imgUrl = $"https://api.dicebear.com/9.x/shapes/svg?seed={payload.username.value}"
          authId = payload.authId
          firstName = payload.firstName
          lastName = payload.lastName
          wants = Set.empty
          location = payload.location
          seeds = Set.empty
          history = List.singleton (CreateUser payload) }

let rec private applyWithoutHistory (event: UserEventPayload) (user: User) =
    let Without plant = Set.filter (fun p -> p.plant <> plant)

    (match event with
     | AggregateEvent events -> events |> List.fold (fun user event -> applyWithoutHistory event user) user
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
             failwith "Cannot apply CreateUser to existing user"
     | SetDawaLocation dawaLocation -> { user with location = dawaLocation })

let apply (event: UserEventPayload) (user: User) =
    { applyWithoutHistory event user with
        history = event :: user.history }

type AlertVariant =
    | SuccessMessage
    | ErrorMessage

type Alert = {
    variant: AlertVariant
    title: string
    message: string
}
