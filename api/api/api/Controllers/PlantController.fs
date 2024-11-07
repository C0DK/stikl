namespace api.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc

open api


[<ApiController>]
[<Route("[controller]")>]
type PlantController(getAll: unit -> domain.Plant List Task, get: domain.PlantId -> domain.Plant Option Task) =
    inherit ControllerBase()



    [<HttpGet>]
    member _.GetAll() =
        getAll () |> (Task.map (List.map Dto.PlantSummary.fromDomain))

    [<HttpGet("{id}")>]
    [<ProducesResponseType(typeof<Dto.User>, 200)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    member _.Get(id: Guid) =
        get id
        |> (Task.map (Option.map Dto.Plant.fromDomain))
        |> (Task.map HttpResult.fromOption)
