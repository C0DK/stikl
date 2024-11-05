namespace api.Controllers

open System
open System.Security.Claims
open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Mvc

open api


[<ApiController>]
[<Route("[controller]")>]
type UserController
    (
        getUsers: unit -> domain.User List Task,
        getUser: domain.UserId -> domain.User Option Task,
        applyEvent: domain.UserEvent -> domain.UserId -> Result<domain.UserEvent, string> Task
    ) =
    inherit ControllerBase()



    [<HttpGet>]
    member _.GetAll() =
        getUsers () |> (Task.map (List.map Dto.User.fromDomain))

    [<HttpGet("{id}")>]
    [<ProducesResponseType(typeof<Dto.User>, 200)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    member _.Get(id: domain.UserId) =
        getUser id
        |> (Task.map (Option.map Dto.User.fromDomain))
        |> (Task.map HttpResult.fromOption)

    member private this.getCurrentUserId() =
        let claim =
            this.User.Claims
            |> Seq.find (fun claim -> claim.Type = ClaimTypes.NameIdentifier)

        claim.Value |> Guid.Parse


    [<HttpPost("AddWant")>]
    [<ProducesResponseType(typeof<string>, 201)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    [<ProducesResponseType(typeof<string>, 400)>]
    [<Authorize>]
    member this.AddWant(plant: domain.PlantId) =
        let userId = this.getCurrentUserId ()

        applyEvent (domain.AddedWant plant) userId
        |> Task.map (Result.map (fun _ -> "Success!") >> HttpResult.fromResult)
