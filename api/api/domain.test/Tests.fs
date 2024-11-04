module Tests

open FsCheck.FSharp
open FsCheck.Xunit
open domain

let withoutNeeds user = { user with needs = Set.empty }
let withoutSeeds user = { user with seeds = Set.empty }

module apply =

    [<Property>]
    let ``Applying event adds it to history`` (user: User) (event: UserEvent) =
        let user = apply event user

        user.history |> List.head = event

    [<Property>]
    let ``If needs, then wants`` plant =
        apply (Needs plant) >> User.Wants plant
        
    [<Property>]
    let ``If seeds, then has`` plant =
        apply (Seeds plant) >> User.Has plant
        
    [<Property>]
    let ``After Needs user needs`` (user: User) (plantId: PlantId) =
        let user = user |> withoutNeeds |> apply (Needs plantId)

        user.needs = Set.singleton plantId

    [<Property>]
    let ``After Seeds user seeds`` (user: User) (plantId: PlantId) =
        let user = user |> withoutSeeds |> apply (Seeds plantId)

        user.seeds = Set.singleton plantId

    [<Property>]
    let ``Can seed multiple`` (user: User) (plantId: PlantId) existingSeeds =
        let user = { user with seeds = existingSeeds } |> apply (Seeds plantId)

        let userHas plant = User.Has plant user

        Set.forall userHas existingSeeds && userHas plantId

    [<Property>]
    let ``need does not change existing`` (user: User) (plantId: PlantId) existingNeeds =
        let user = { user with needs = existingNeeds } |> apply (Needs plantId)

        let userWants plant = User.Wants plant user

        Set.forall userWants existingNeeds && userWants plantId


    [<Property>]
    let ``After NoLongerNeeds user doesnt need`` (user: User) (plantId: PlantId) =
        let user = user |> apply (NoLongerNeeds plantId)

        user |> User.Wants plantId |> not

    [<Property>]
    let ``After NoLongerSeeds user doesnt seed`` (user: User) (plantId: PlantId) =
        let user = user |> apply (NoLongerSeeds plantId)

        user |> User.Has plantId |> not

module DoesNeed =

    [<Property>]
    let ``Returns true if in set`` (user: User) (plantId: PlantId) =
        { user with
            needs = Set.singleton plantId }
        |> User.Wants plantId

    [<Property>]
    let ``Returns false for empty set`` (user: User) (plantId: PlantId) =
        { user with needs = Set.empty } |> User.Wants plantId |> not

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
    let ``Returns false for empty set`` (user: User) (plantId: PlantId) =
        { user with seeds = Set.empty } |> User.Has plantId |> not

    [<Property>]
    let ``Returns false for other plant id`` (user: User) (plantId: PlantId) (otherPlantId: PlantId) =
        (plantId <> otherPlantId)
        ==> ({ user with
                 seeds = Set.singleton otherPlantId }
             |> User.Has plantId
             |> not)
