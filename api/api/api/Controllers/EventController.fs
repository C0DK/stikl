namespace api.Controllers

open System
open System.Security.Claims
open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Mvc

open api


[<ApiController>]
[<Route("[controller]")>]
[<Authorize>]
type EventController(applyEvent: domain.UserEvent -> domain.UserId -> Result<domain.UserEvent, string> Task) =
    inherit ControllerBase()


    member private this.getCurrentUserId() =
        let claim =
            this.User.Claims
            |> Seq.find (fun claim -> claim.Type = ClaimTypes.NameIdentifier)

        claim.Value |> domain.UserId


    [<HttpPost("AddWant")>]
    [<ProducesResponseType(typeof<string>, 201)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    [<ProducesResponseType(typeof<string>, 400)>]
    member this.AddWant(plant: domain.PlantId) =
        let userId = CurrentUser.get this

        applyEvent (domain.AddedWant plant) userId
        |> Task.map (Result.map (fun _ -> "Success!") >> HttpResult.fromResult)
