module Tests

open FsCheck.FSharp
open FsCheck.Xunit
open domain

let withNoWants user = { user with wants = Set.empty }
let withNoSeeds user = { user with seeds = Set.empty }

module apply =

    let isIdempotent func check user = user |> func |> check = check user

    [<Property>]
    let ``AddedWant + RemovedWant are idempotent`` user plant =
        not (User.Wants plant.id user)
        ==> isIdempotent (apply (AddedWant plant) >> apply (RemovedWant plant)) User.GetWants user

    [<Property>]
    let ``AddedSeeds + RemovedSeeds are idempotent`` user plant =
        not (User.Has plant.plant.id user)
        ==> isIdempotent (apply (AddedSeeds plant) >> apply (RemovedSeeds plant.plant)) User.GetSeeds user


    [<Property>]
    let ``Applying event adds it to history`` user event =
        let user = apply event user

        user.history |> List.head = event

    [<Property>]
    let ``If AddedWant, then wants`` plant =
        apply (AddedWant plant) >> User.Wants plant.id

    [<Property>]
    let ``If seeds, then has`` plant =
        apply (AddedSeeds plant) >> User.Has plant.plant.id

    [<Property>]
    let ``If removed want, then not wants`` plant =
        apply (RemovedWant plant) >> User.Wants plant.id >> not

    [<Property>]
    let ``If no longer seeds, then not has`` plant =
        apply (RemovedSeeds plant) >> User.Has plant.id >> not

    [<Property>]
    let ``After AddedWant user wants`` user plantId =
        user |> withNoWants |> apply (AddedWant plantId) |> User.GetWants = Set.singleton plantId

    [<Property>]
    let ``After AddedSeeds user seeds`` (user: User) plant =
        user |> withNoSeeds |> apply (AddedSeeds plant) |> User.GetSeeds = Set.singleton plant

    [<Property>]
    let ``AddedSeeds does not change existing`` (user: User) plant existingSeeds =
        let user = { user with seeds = existingSeeds } |> apply (AddedSeeds plant)

        let userHas plant = User.Has plant.id user

        Set.forall userHas (existingSeeds |> Set.map _.plant) && userHas plant.plant

    [<Property>]
    let ``AddedWant does not change existing`` (user: User) (plant: Plant) plants =
        let user = { user with wants = plants } |> apply (AddedWant plant)

        let userWants plant = User.Wants plant.id user

        Set.forall userWants plants && userWants plant


module Wants =

    [<Property>]
    let ``Returns true if in set`` (user: User) (plant) =
        { user with
            wants = Set.singleton plant }
        |> User.Wants plant.id

    [<Property>]
    let ``Returns false for empty set`` (plantId: PlantId) =
        withNoWants >> User.Wants plantId >> not

    [<Property>]
    let ``Returns false for other plant id`` (user: User) (plant: Plant) (otherPlant: Plant) =
        (plant <> otherPlant)
        ==> ({ user with
                 wants = Set.singleton otherPlant }
             |> User.Wants plant.id
             |> not)

module Has =

    [<Property>]
    let ``Returns true if in set`` (user: User) plant =
        { user with
            seeds = Set.singleton plant }
        |> User.Has plant.plant.id

    [<Property>]
    let ``Returns false for empty set`` (plantId: PlantId) = withNoSeeds >> User.Has plantId >> not

    [<Property>]
    let ``Returns false for other plant id`` (user: User) (plant) (otherPlant) =
        (plant <> otherPlant)
        ==> ({ user with
                 seeds = Set.singleton otherPlant }
             |> User.Has plant.plant.id
             |> not)
