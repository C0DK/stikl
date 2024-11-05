module Tests

open FsCheck.FSharp
open FsCheck.Xunit
open domain

let withNoNeeds user = { user with needs = Set.empty }
let withNoSeeds user = { user with seeds = Set.empty }

module apply =

    let isIdempotent func check user = user |> func |> check = check user

    [<Property>]
    let ``Needs + NoLongerNeeds are idempotent`` user plant =
        not (User.Wants plant user)
        ==> isIdempotent (apply (Needs plant) >> apply (NoLongerNeeds plant)) User.GetNeeds user

    [<Property>]
    let ``Seeds + NoLongerSeeds are idempotent`` user plant =
        not (User.Has plant user)
        ==> isIdempotent (apply (Seeds plant) >> apply (NoLongerSeeds plant)) User.GetSeeds user


    [<Property>]
    let ``Applying event adds it to history`` user event =
        let user = apply event user

        user.history |> List.head = event

    [<Property>]
    let ``If needs, then wants`` plant = apply (Needs plant) >> User.Wants plant

    [<Property>]
    let ``If seeds, then has`` plant = apply (Seeds plant) >> User.Has plant

    [<Property>]
    let ``If no longer needs, then not wants`` plant =
        apply (NoLongerNeeds plant) >> User.Wants plant >> not

    [<Property>]
    let ``If no longer seeds, then not has`` plant =
        apply (NoLongerSeeds plant) >> User.Has plant >> not

    [<Property>]
    let ``After Needs user needs`` user plantId =
        user |> withNoNeeds |> apply (Needs plantId) |> User.GetNeeds = Set.singleton plantId

    [<Property>]
    let ``After Seeds user seeds`` (user: User) (plantId: PlantId) =
        user |> withNoSeeds |> apply (Seeds plantId) |> User.GetSeeds = Set.singleton plantId

    [<Property>]
    let ``Can seed multiple`` (user: User) (plantId: PlantId) existingSeeds =
        let user = { user with seeds = existingSeeds } |> apply (Seeds plantId)

        let userHas plant = User.Has plant user

        Set.forall userHas existingSeeds && userHas plantId

    [<Property>]
    let ``need does not change existing`` (user: User) (plantId: PlantId) plants =
        let user = { user with needs = plants } |> apply (Needs plantId)

        let userWants plant = User.Wants plant user

        Set.forall userWants plants && userWants plantId


module DoesNeed =

    [<Property>]
    let ``Returns true if in set`` (user: User) (plantId: PlantId) =
        { user with
            needs = Set.singleton plantId }
        |> User.Wants plantId

    [<Property>]
    let ``Returns false for empty set`` (plantId: PlantId) =
        withNoNeeds >> User.Wants plantId >> not

    [<Property>]
    let ``Returns false for other plant id`` (user: User) (plantId: PlantId) (otherPlantId: PlantId) =
        (plantId <> otherPlantId)
        ==> ({ user with
                 needs = Set.singleton otherPlantId }
             |> User.Wants plantId
             |> not)

module DoesSeed =

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
