module Event

open System.Net
open Xunit
open api
open api.test

let plant = Composition.basil

let clientWithPlantAndUser user =
    APIClient.withUsers (List.singleton user)
    >> APIClient.withPlants (List.singleton plant)
    |> APIClient.build

let clientWithUser user =
    APIClient.withUsers (List.singleton user) |> APIClient.build

module AddWant =
    let user = domain.User.createRandom ()

    [<Fact>]
    let ``fails if user does not exist`` () =
        let client = APIClient.plainClient ()
        client |> Http.loginAs user.id

        client
        |> Http.postJson "/event/AddWant" { Dto.PlantRequest.plantId = plant.id }
        |> Assert.asyncHasStatusCode HttpStatusCode.BadRequest

    [<Fact>]
    let ``fails if plant does not exist`` () =
        let client = clientWithUser user
        client |> Http.loginAs user.id

        client
        |> Http.postJson "/event/AddWant" { Dto.PlantRequest.plantId = domain.PlantId.create() }
        |> Assert.asyncHasStatusCode HttpStatusCode.NotFound

    [<Fact>]
    let ``Updates user, given logged in`` () =
        task {
            let client = clientWithPlantAndUser user
            client |> Http.loginAs user.id

            do!
                client
                |> Http.postJson "/event/AddWant" { Dto.PlantRequest.plantId = plant.id }
                |> Assert.hasStatusCodeOk

            let! result = client |> Http.getJson<Dto.User> $"/User/{user.id.value}/"

            Assert.Contains(plant.id, result.wants)

        }

    [<Fact>]
    let ``fails if not logged in`` () =
        let client = APIClient.plainClient ()

        client
        |> Http.postJson "/event/AddWant" { Dto.PlantRequest.plantId = plant.id }
        |> Assert.asyncHasStatusCode HttpStatusCode.Unauthorized

module AddSeeds =
    let user = domain.User.createRandom ()

    [<Fact>]
    let ``Updates user, given logged in`` () =
        task {
            let client = clientWithPlantAndUser user
            client |> Http.loginAs user.id

            do!
                client
                |> Http.postJson "/event/AddSeeds" { Dto.PlantRequest.plantId = plant.id }
                |> Assert.hasStatusCodeOk

            let! result = client |> Http.getJson<Dto.User> $"/User/{user.id.value}/"

            Assert.Contains(plant.id, result.seeds)

        }

module RemoveSeeds =

    let userWithPlant =
        domain.User.createRandom () |> domain.apply (domain.AddedSeeds plant.id)

    [<Fact>]
    let ``Updates user, given logged in`` () =
        task {
            Assert.Contains(plant.id, userWithPlant.seeds)

            let client = clientWithPlantAndUser userWithPlant

            client |> Http.loginAs userWithPlant.id

            do!
                client
                |> Http.postJson "/event/RemoveSeeds" { Dto.PlantRequest.plantId = plant.id }
                |> Assert.hasStatusCodeOk

            let! result = client |> Http.getJson<Dto.User> $"/User/{userWithPlant.id.value}/"

            Assert.DoesNotContain(plant.id, result.seeds)

        }

module RemoveNeeds =

    let userWithPlant =
        domain.User.createRandom () |> domain.apply (domain.AddedWant plant.id)

    [<Fact>]
    let ``Updates user, given logged in`` () =
        task {
            Assert.Contains(plant.id, userWithPlant.wants)

            let client = clientWithPlantAndUser userWithPlant
            client |> Http.loginAs userWithPlant.id

            do!
                client
                |> Http.postJson "/event/RemoveWant" { Dto.PlantRequest.plantId = plant.id }
                |> Assert.hasStatusCodeOk

            let! result = client |> Http.getJson<Dto.User> $"/User/{userWithPlant.id.value}/"

            Assert.DoesNotContain(plant.id, result.wants)

        }
