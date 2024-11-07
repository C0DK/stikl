namespace api.Controllers

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
    member this.AddWant([<FromBody>] payload: Dto.PlantRequest) =
        match CurrentUser.get this with
        | Ok userId ->
            applyEvent (domain.AddedWant payload.plantId) userId
            |> Task.map (Result.map (fun _ -> "Success!") >> HttpResult.fromResult)
        | Error message -> HttpResult.badRequest message |> Task.FromResult

    [<HttpPost("AddSeeds")>]
    [<ProducesResponseType(typeof<string>, 201)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    [<ProducesResponseType(typeof<string>, 400)>]
    member this.AddSeeds([<FromBody>] payload: Dto.PlantRequest) =
        match CurrentUser.get this with
        | Ok userId ->
            applyEvent (domain.AddedSeeds payload.plantId) userId
            |> Task.map (Result.map (fun _ -> "Success!") >> HttpResult.fromResult)
        | Error message -> HttpResult.badRequest message |> Task.FromResult
        
    [<HttpPost("RemoveSeeds")>]
    [<ProducesResponseType(typeof<string>, 201)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    [<ProducesResponseType(typeof<string>, 400)>]
    member this.RemoveSeeds([<FromBody>] payload: Dto.PlantRequest) =
        match CurrentUser.get this with
        | Ok userId ->
            applyEvent (domain.RemovedSeeds payload.plantId) userId
            |> Task.map (Result.map (fun _ -> "Success!") >> HttpResult.fromResult)
        | Error message -> HttpResult.badRequest message |> Task.FromResult
        
    [<HttpPost("RemoveWant")>]
    [<ProducesResponseType(typeof<string>, 201)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    [<ProducesResponseType(typeof<string>, 400)>]
    member this.RemoveWant([<FromBody>] payload: Dto.PlantRequest) =
        match CurrentUser.get this with
        | Ok userId ->
            applyEvent (domain.RemovedWant payload.plantId) userId
            |> Task.map (Result.map (fun _ -> "Success!") >> HttpResult.fromResult)
        | Error message -> HttpResult.badRequest message |> Task.FromResult
