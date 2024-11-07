namespace api.Controllers

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

    let assertExists plantId =
        exists plantId
        |> Task.map (fun b ->
            if b then
                Ok plantId
            else
                Error(HttpError.NotFound $"Plant '{plantId}' not found"))

    member private this.handleEventForCurrentUser event =
        CurrentUser.get this
        |> Result.map (
            (applyEvent event)
            >> (Task.map (fun result ->
                match result with
                | Ok _ -> HttpResult.ok "Event handled!"
                | Error msg -> HttpError.BadRequest msg |> HttpError.toHttpResult))
        )
        |> Task.unpackResult

    member private this.handlePlantEvent plantId eventType =
        assertExists plantId
        |> Task.collect (
            Result.map (eventType >> this.handleEventForCurrentUser)
            >> Task.unpackResult
            >> Task.map Result.unpack
        )
        |> Task.map HttpError.resultToHttpResult


    [<HttpPost("AddWant")>]
    [<ProducesResponseType(typeof<string>, 201)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    [<ProducesResponseType(typeof<string>, 400)>]
    member this.AddWant([<FromBody>] payload: Dto.PlantRequest) =
        this.handlePlantEvent payload.plantId domain.AddedWant


    [<HttpPost("AddSeeds")>]
    [<ProducesResponseType(typeof<string>, 201)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    [<ProducesResponseType(typeof<string>, 400)>]
    member this.AddSeeds([<FromBody>] payload: Dto.PlantRequest) =
        this.handlePlantEvent payload.plantId domain.AddedSeeds

    [<HttpPost("RemoveSeeds")>]
    [<ProducesResponseType(typeof<string>, 201)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    [<ProducesResponseType(typeof<string>, 400)>]
    member this.RemoveSeeds([<FromBody>] payload: Dto.PlantRequest) =
        this.handlePlantEvent payload.plantId domain.RemovedSeeds

    [<HttpPost("RemoveWant")>]
    [<ProducesResponseType(typeof<string>, 201)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    [<ProducesResponseType(typeof<string>, 400)>]
    member this.RemoveWant([<FromBody>] payload: Dto.PlantRequest) =
        this.handlePlantEvent payload.plantId domain.RemovedWant
