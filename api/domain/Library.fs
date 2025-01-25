module domain

open System

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

// We need to figure out what type of user id - dotnet-jwt gives a string of the username. auth0 probably same
// TODO: ensure UserId is a string of no spaces etc.
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

type PlantOffer = {
    plant: Plant
    comment: string option
    seedKind: SeedKind
}

// TODO: is it best / bad to include the whole plant in the event??
type UserEvent =
    | AddedWant of Plant
    | AddedSeeds of PlantOffer
    | RemovedWant of Plant
    | RemovedSeeds of Plant

type User =
    { username: Username
      imgUrl: string
      firstName: string option
      fullName: string option
      wants: Plant Set
      seeds: PlantOffer Set
      history: UserEvent list }




module User =
    let Wants (id: PlantId) user =
        user.wants |> Set.exists (fun p -> p.id = id)

    let Has id user =
        user.seeds |> Set.exists (fun p -> p.plant.id = id)

    let GetWants user = user.wants

    let GetSeeds user = user.seeds

    let create id =
        { username = id
          imgUrl = "https://cdn5.vectorstock.com/i/1000x1000/74/34/no-user-sign-icon-person-symbol-vector-1907434.jpg"
          firstName = None
          fullName = None
          wants = Set.empty
          seeds = Set.empty
          history = List.empty }


    let createRandom () = create Username.random

let apply (event: UserEvent) (user: User) =
    let Without plant =
        Set.filter (fun p -> p.plant <> plant)
        
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
                 seeds =  user.seeds |> Without plant })

    { user with
        history = event :: user.history }
