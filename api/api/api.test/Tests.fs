module Tests

open System.Net
open System.Net.Http
open System.Net.Http.Json
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Xunit
open FsCheck.FSharp
open FsCheck.Xunit
open api.test
open api.Controllers

module Assert =
    let hasStatusCode (expected: HttpStatusCode) (response: HttpResponseMessage Task) =
        task {
            let! response = response 
            Assert.Equal(expected, response.StatusCode)
        }
        
module Http =
   let get (path: string) (client: HttpClient) = 
        client.GetAsync(path)

[<Property>]
let ``Can get swagger`` =
    APIClient.getClientWithUsers >> Http.get "/Swagger" >> Assert.hasStatusCode HttpStatusCode.OK
    
[<Property>]
let ``GetAll returns all entries`` users =
    task {
        let client = APIClient.getClientWithUsers users
        
        let! result  = client.GetFromJsonAsync<List<UserDto>>("/User/")
    
        let expected = users |> List.map UserDto.fromDomain 
        Assert.Equivalent(expected, result)
    }
[<Property>]
let ``GetUser returns user if in list`` users user =
    task {
        let users = user :: users
        let client = APIClient.getClientWithUsers users
        
        let! result  = client.GetFromJsonAsync<UserDto>($"/User/{user.id.ToString()}/")
    
        let expected = UserDto.fromDomain user
        Assert.Equal(expected, result)
    }
[<Property>]
let ``GetUser returns 404 if not in list`` users user =
    not (List.contains user users) ==> task {
        let client = APIClient.getClientWithUsers users
        
        let! response  = client.GetAsync($"/User/{user.id.ToString()}/")
    
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode)
    }
