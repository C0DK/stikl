module Tests

open System.Net
open Xunit
open FsCheck.FSharp
open FsCheck.Xunit
open api
open api.test


[<Property>]
let ``Can get swagger`` =
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
