module Tests

open System.Net
open System.Net.Http
open System.Net.Http.Json
open System.Threading.Tasks
open Xunit
open FsCheck.FSharp
open FsCheck.Xunit
open api.test
open api.Controllers

module Assert =
    let equal (a: 'T) (b: 'T) = Assert.Equal<'T>(a, b)

    let hasStatusCode (expected: HttpStatusCode) (response: HttpResponseMessage Task) =
        task {
            let! response = response
            equal expected response.StatusCode
        }

module Http =
    let get (path: string) (client: HttpClient) = client.GetAsync(path)
    let getJson<'T> (path: string) (client: HttpClient) = client.GetFromJsonAsync<'T>(path)

[<Property>]
let ``Can get swagger`` =
    APIClient.getClientWithUsers
    >> Http.get "/Swagger"
    >> Assert.hasStatusCode HttpStatusCode.OK

[<Property>]
let ``GetAll returns all entries`` users =
    task {
        let client = APIClient.getClientWithUsers users

        let! result = client.GetFromJsonAsync<List<UserDto>>("/User/")

        let expected = users |> List.map UserDto.fromDomain
        Assert.Equivalent(expected, result)
    }

[<Property>]
let ``GetUser returns user if in list`` users user =
    let client = APIClient.getClientWithUsers (user :: users)

    client
    |> Http.getJson<UserDto> $"/User/{user.id.ToString()}/"
    |> Task.map (Assert.equal (UserDto.fromDomain user))

[<Property>]
let ``GetUser returns 404 if not in list`` users user =
    not (List.contains user users)
    ==> (let client = APIClient.getClientWithUsers users

         client
         |> Http.get $"/User/{user.id.ToString()}/"
         |> Assert.hasStatusCode HttpStatusCode.NotFound)
