module Tests

open System.Net
open Xunit
open FsCheck.FSharp
open FsCheck.Xunit
open api
open api.test


[<Property>]
let ``Can get swagger`` () =
    APIClient.getClientWithUsers
    >> Http.get "/Swagger"
    >> Assert.hasStatusCode HttpStatusCode.OK

[<Property>]
let ``GetAll returns all entries`` users =
    task {
        let client = APIClient.getClientWithUsers users

        let! result = client |> Http.getJson<List<Dto.User>> ("/User/")

        let expected = users |> List.map Dto.User.fromDomain
        Assert.Equivalent(expected, result)
    }

[<Property>]
let ``GetUser returns user if in list`` users user =
    let client = APIClient.getClientWithUsers (user :: users)

    client
    |> Http.getJson<Dto.User> $"/User/{user.id.ToString()}/"
    |> Task.map (Assert.equal (Dto.User.fromDomain user))

[<Property>]
let ``GetUser returns 404 if not in list`` users user =
    not (List.contains user users)
    ==> (let client = APIClient.getClientWithUsers users

         client
         |> Http.get $"/User/{user.id.ToString()}/"
         |> Assert.hasStatusCode HttpStatusCode.NotFound)


module AddWant =
    [<Property>]
    let ``fails if plant does not exist`` () = failwith "TODO"

    [<Property>]
    let ``Updates user, given logged in`` user plantId =
        task {
            let client = APIClient.getClientWithUsers (List.singleton user)

            do!
                client
                |> Http.post $"/User/{user.id.ToString()}/" { Dto.AddWants.plantId = plantId }
                |> Assert.hasStatusCodeOk

            let! result = client |> Http.getJson<Dto.User> $"/User/{user.id.ToString()}/"

            Assert.Contains(plantId, result.needs)
        }

    [<Fact>]
    let ``fails if not logged in`` () =
        let user = domain.User.createNew ()
        let plantId = domain.PlantId.create ()
        let client = APIClient.getClient ()

        client
        |> Http.post $"/User/{user.id.ToString()}/" { Dto.AddWants.plantId = plantId }
        |> Assert.hasStatusCode HttpStatusCode.Unauthorized
