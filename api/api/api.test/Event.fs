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
        |> Http.postJson "/event/AddWant" { Dto.AddWant.plantId = plantId }
        |> Assert.asyncHasStatusCode HttpStatusCode.BadRequest

    [<Fact>]
    let ``Updates user, given logged in`` () =
        task {
            let client = APIClient.getClientWithUsers (List.singleton user)
            client |> Http.loginAs user.id

            do!
                client
                |> Http.postJson "/event/AddWant" { Dto.AddWant.plantId = plantId }
                |> Assert.hasStatusCodeOk

            let! result = client |> Http.getJson<Dto.User> $"/User/{user.id.value}/"

            Assert.Contains(plantId, result.needs)

        }

    [<Fact>]
    let ``fails if not logged in`` () =
        let plantId = domain.PlantId.create ()
        let client = APIClient.getClient ()

        client
        |> Http.postJson "/event/AddWant" { Dto.AddWant.plantId = plantId }
        |> Assert.asyncHasStatusCode HttpStatusCode.Unauthorized
