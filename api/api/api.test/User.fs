module User

open System.Net
open Xunit
open FsCheck.FSharp
open FsCheck.Xunit
open api
open api.test

let clientWithUsers = APIClient.withUsers >> APIClient.build

[<Fact>]
let ``Can get swagger`` () =
    APIClient.getClientWithUsers
    >> Http.get "/Swagger"
    >> Assert.asyncHasStatusCode HttpStatusCode.OK

[<Arbitrary.Property>]
let ``GetAll returns all entries`` users =
    task {
        let client = clientWithUsers users

        let! result = client |> Http.getJson<List<Dto.User>> "/User/"

        let expected = users |> List.map Dto.User.fromDomain
        Assert.Equivalent(expected, result)
    }

[<Arbitrary.Property>]
let ``GetUser returns user if in list`` users (user: domain.User) =
    let client = clientWithUsers (user :: users) in

    client
    |> Http.getJson<Dto.User> $"/User/{user.id.value}"
    |> Task.map (Assert.equal (Dto.User.fromDomain user))

[<Arbitrary.Property>]
let ``GetUser returns 404 if not in list`` users user =
    not (List.contains user users)
    ==> (let client = clientWithUsers users

         client
         |> Http.get $"/User/{user.id.value}/"
         |> Assert.asyncHasStatusCode HttpStatusCode.NotFound)


module Create =

    let user = domain.User.createRandom ()

    [<Fact>]
    let ``Successful with new user`` () =
        task {
            let client = APIClient.plainClient ()
            client |> Http.loginAs user.id

            let! createdResponse = client |> Http.postEmpty "/User/"

            createdResponse |> Assert.hasStatusCode HttpStatusCode.Created

            let newLocation = createdResponse.Headers.Location.AbsolutePath

            Assert.equal $"/User/{user.id.value}" newLocation

            let! result = client |> Http.getJson<Dto.User> newLocation
            Assert.equal user.id.value result.id
        }

    [<Fact>]
    let ``fails if user already exists`` () =
        let client = clientWithUsers (List.singleton user)
        client |> Http.loginAs user.id

        client
        |> Http.postEmpty "/User/"
        |> Assert.asyncHasStatusCode HttpStatusCode.Conflict

    [<Fact>]
    let ``fails if not logged in`` () =
        let client = clientWithUsers (List.singleton user)

        client
        |> Http.postEmpty "/User/"
        |> Assert.asyncHasStatusCode HttpStatusCode.Unauthorized
