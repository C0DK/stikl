namespace api.Controllers

open System.Security.Claims
open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Mvc

open api



[<ApiController>]
[<Route("[controller]")>]
[<Authorize>]
type EventController
    (
        applyEvent: domain.UserEvent -> domain.UserId -> Result<domain.UserEvent, string> Task,
        exists: domain.PlantId -> bool Task
    ) =
    inherit ControllerBase()

    let ifExistsThen plantId v =
        exists plantId
        |> Task.collect (fun b ->
            if b then
                v
            else
                HttpResult.notFound "Plant not found" |> Task.FromResult)

    member this.handleEventForCurrentUser event =
        // TODO: create a httpError railroad kinda thing.
        match CurrentUser.get this with
        | Ok userId ->
            applyEvent event userId
            |> Task.map (Result.map (fun _ -> "Success!") >> HttpResult.fromResult)
        | Error message -> HttpResult.badRequest message |> Task.FromResult




    [<HttpPost("AddWant")>]
    [<ProducesResponseType(typeof<string>, 201)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    [<ProducesResponseType(typeof<string>, 400)>]
    member this.AddWant([<FromBody>] payload: Dto.PlantRequest) =
        ifExistsThen payload.plantId (this.handleEventForCurrentUser (domain.AddedWant payload.plantId))

    [<HttpPost("AddSeeds")>]
    [<ProducesResponseType(typeof<string>, 201)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    [<ProducesResponseType(typeof<string>, 400)>]
    member this.AddSeeds([<FromBody>] payload: Dto.PlantRequest) =
        ifExistsThen payload.plantId (this.handleEventForCurrentUser (domain.AddedSeeds payload.plantId))

    [<HttpPost("RemoveSeeds")>]
    [<ProducesResponseType(typeof<string>, 201)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    [<ProducesResponseType(typeof<string>, 400)>]
    member this.RemoveSeeds([<FromBody>] payload: Dto.PlantRequest) =
        ifExistsThen payload.plantId (this.handleEventForCurrentUser (domain.RemovedSeeds payload.plantId))

    [<HttpPost("RemoveWant")>]
    [<ProducesResponseType(typeof<string>, 201)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    [<ProducesResponseType(typeof<string>, 400)>]
    member this.RemoveWant([<FromBody>] payload: Dto.PlantRequest) =
        ifExistsThen payload.plantId (this.handleEventForCurrentUser (domain.RemovedWant payload.plantId))
