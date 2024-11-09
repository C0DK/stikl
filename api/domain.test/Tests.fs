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
        not (User.Wants plant user)
        ==> isIdempotent (apply (AddedWant plant) >> apply (RemovedWant plant)) User.GetWants user

    [<Property>]
    let ``AddedSeeds + RemovedSeeds are idempotent`` user plant =
        not (User.Has plant user)
        ==> isIdempotent (apply (AddedSeeds plant) >> apply (RemovedSeeds plant)) User.GetSeeds user


    [<Property>]
    let ``Applying event adds it to history`` user event =
        let user = apply event user

        user.history |> List.head = event

    [<Property>]
    let ``If AddedWant, then wants`` plant =
        apply (AddedWant plant) >> User.Wants plant

    [<Property>]
    let ``If seeds, then has`` plant =
        apply (AddedSeeds plant) >> User.Has plant

    [<Property>]
    let ``If removed want, then not wants`` plant =
        apply (RemovedWant plant) >> User.Wants plant >> not

    [<Property>]
    let ``If no longer seeds, then not has`` plant =
        apply (RemovedSeeds plant) >> User.Has plant >> not

    [<Property>]
    let ``After AddedWant user wants`` user plantId =
        user |> withNoWants |> apply (AddedWant plantId) |> User.GetWants = Set.singleton plantId

    [<Property>]
    let ``After AddedSeeds user seeds`` (user: User) (plantId: PlantId) =
        user |> withNoSeeds |> apply (AddedSeeds plantId) |> User.GetSeeds = Set.singleton plantId

    [<Property>]
    let ``AddedSeeds does not change existing`` (user: User) (plantId: PlantId) existingSeeds =
        let user = { user with seeds = existingSeeds } |> apply (AddedSeeds plantId)

        let userHas plant = User.Has plant user

        Set.forall userHas existingSeeds && userHas plantId

    [<Property>]
    let ``AddedWant does not change existing`` (user: User) (plantId: PlantId) plants =
        let user = { user with wants = plants } |> apply (AddedWant plantId)

        let userWants plant = User.Wants plant user

        Set.forall userWants plants && userWants plantId


module Wants =

    [<Property>]
    let ``Returns true if in set`` (user: User) (plantId: PlantId) =
        { user with
            wants = Set.singleton plantId }
        |> User.Wants plantId

    [<Property>]
    let ``Returns false for empty set`` (plantId: PlantId) =
        withNoWants >> User.Wants plantId >> not

    [<Property>]
    let ``Returns false for other plant id`` (user: User) (plantId: PlantId) (otherPlantId: PlantId) =
        (plantId <> otherPlantId)
        ==> ({ user with
                 wants = Set.singleton otherPlantId }
             |> User.Wants plantId
             |> not)

module Has =

    [<Property>]
    let ``Returns true if in set`` (user: User) (plantId: PlantId) =
        { user with
            seeds = Set.singleton plantId }
        |> User.Has plantId

    [<Property>]
    let ``Returns false for empty set`` (plantId: PlantId) = withNoSeeds >> User.Has plantId >> not

    [<Property>]
    let ``Returns false for other plant id`` (user: User) (plantId: PlantId) (otherPlantId: PlantId) =
        (plantId <> otherPlantId)
        ==> ({ user with
                 seeds = Set.singleton otherPlantId }
             |> User.Has plantId
             |> not)
