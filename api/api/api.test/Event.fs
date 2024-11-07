module Event

open System
open System.Net
open Xunit
open api
open api.test


module AddWant =
    let user = domain.User.createRandom ()
    let plantId = Guid.NewGuid()

    [<Fact>]
    let ``fails if user does not exist`` () =
        let client = APIClient.getClient ()
        client |> Http.loginAs user.id

        client
        |> Http.postJson "/event/AddWant" { Dto.PlantRequest.plantId = plantId }
        |> Assert.asyncHasStatusCode HttpStatusCode.BadRequest

    [<Fact>]
    let ``Updates user, given logged in`` () =
        task {
            let client = APIClient.getClientWithUsers (List.singleton user)
            client |> Http.loginAs user.id

            do!
                client
                |> Http.postJson "/event/AddWant" { Dto.PlantRequest.plantId = plantId }
                |> Assert.hasStatusCodeOk

            let! result = client |> Http.getJson<Dto.User> $"/User/{user.id.value}/"

            Assert.Contains(plantId, result.wants)

        }

    [<Fact>]
    let ``fails if not logged in`` () =
        let plantId = domain.PlantId.create ()
        let client = APIClient.getClient ()

        client
        |> Http.postJson "/event/AddWant" { Dto.PlantRequest.plantId = plantId }
        |> Assert.asyncHasStatusCode HttpStatusCode.Unauthorized

module AddSeeds =
    let user = domain.User.createRandom ()
    let plantId = Guid.NewGuid()

    [<Fact>]
    let ``Updates user, given logged in`` () =
        task {
            let client = APIClient.getClientWithUsers (List.singleton user)
            client |> Http.loginAs user.id

            do!
                client
                |> Http.postJson "/event/AddSeeds" { Dto.PlantRequest.plantId = plantId }
                |> Assert.hasStatusCodeOk

            let! result = client |> Http.getJson<Dto.User> $"/User/{user.id.value}/"

            Assert.Contains(plantId, result.seeds)

        }

module RemoveSeeds =
    let plantId = Guid.NewGuid()

    let userWithPlant =
        domain.User.createRandom () |> domain.apply (domain.AddedSeeds plantId)

    [<Fact>]
    let ``Updates user, given logged in`` () =
        task {
            Assert.Contains(plantId, userWithPlant.seeds)

            let client = APIClient.getClientWithUsers (List.singleton userWithPlant)
            client |> Http.loginAs userWithPlant.id

            do!
                client
                |> Http.postJson "/event/RemoveSeeds" { Dto.PlantRequest.plantId = plantId }
                |> Assert.hasStatusCodeOk

            let! result = client |> Http.getJson<Dto.User> $"/User/{userWithPlant.id.value}/"

            Assert.DoesNotContain(plantId, result.seeds)

        }

module RemoveNeeds =
    let plantId = Guid.NewGuid()

    let userWithPlant =
        domain.User.createRandom () |> domain.apply (domain.AddedWant plantId)

    [<Fact>]
    let ``Updates user, given logged in`` () =
        task {
            Assert.Contains(plantId, userWithPlant.wants)

            let client = APIClient.getClientWithUsers (List.singleton userWithPlant)
            client |> Http.loginAs userWithPlant.id

            do!
                client
                |> Http.postJson "/event/RemoveWant" { Dto.PlantRequest.plantId = plantId }
                |> Assert.hasStatusCodeOk

            let! result = client |> Http.getJson<Dto.User> $"/User/{userWithPlant.id.value}/"

            Assert.DoesNotContain(plantId, result.wants)

        }
