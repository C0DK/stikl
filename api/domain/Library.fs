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

type UserEvent =
    | AddedWant of PlantId
    | AddedSeeds of PlantId
    | RemovedWant of PlantId
    | RemovedSeeds of PlantId

type User =
    { username: string
      imgUrl: string
      firstName: string option
      fullName: string option
      wants: Plant list
      seeds: Plant list }




module User =
    let Wants plantId user = Set.contains plantId user.wants

    let Has plantId user = Set.contains plantId user.seeds

    let GetWants user = user.wants

    let GetSeeds user = user.seeds

    let create id =
        { username = id
          wants = Set.empty
          seeds = Set.empty
          history = List.empty }


    let createRandom () = create Username.random

let apply (event: UserEvent) (user: User) =
    let user =
        (match event with
         | AddedWant plantId ->
             { user with
                 wants = Set.add plantId user.wants }
         | AddedSeeds plantId ->
             { user with
                 seeds = Set.add plantId user.seeds }
         | RemovedWant plantId ->
             { user with
                 wants = Set.remove plantId user.wants }
         | RemovedSeeds plantId ->
             { user with
                 seeds = Set.remove plantId user.seeds })

    { user with
        history = event :: user.history }
