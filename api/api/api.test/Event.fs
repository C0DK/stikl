module Event

open System.Net
open Xunit
open FsCheck.FSharp
open FsCheck.Xunit
open api
open api.test


module AddWant =
    [<Property>]
    let ``fails if user does not exist`` () = failwith "TODO"

    [<Property>]
    let ``fails if plant does not exist`` () = failwith "TODO"

    [<Property>]
    let ``Updates user, given logged in`` user plantId =
        task {
            let client = APIClient.getClientWithUsers (List.singleton user)
            client |> Http.loginAs user.id

            do!
                client
                |> Http.postJson "/event/AddWant" { Dto.AddWants.plantId = plantId }
                |> Assert.hasStatusCodeOk

            let! result = client |> Http.getJson<Dto.User> $"/User/{user.id.ToString()}/"

            Assert.Contains(plantId, result.needs)
        }

    [<Fact>]
    let ``fails if not logged in`` () =
        let plantId = domain.PlantId.create ()
        let client = APIClient.getClient ()

        client
        |> Http.postJson "/event/AddWant" { Dto.AddWants.plantId = plantId }
        |> Assert.hasStatusCode HttpStatusCode.Unauthorized
