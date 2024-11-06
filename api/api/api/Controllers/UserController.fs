namespace api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc

open api


[<ApiController>]
[<Route("[controller]")>]
type UserController
    (
        getUsers: unit -> domain.User List Task,
        getUser: domain.UserId -> domain.User Option Task,
        createUser: domain.UserId -> Result<unit, string> Task
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

    [<HttpPost>]
    [<ProducesResponseType(201)>]
    [<ProducesResponseType(typeof<string>, 400)>]
    member this.Create() =
        createUser (CurrentUser.get this) |> (Task.map HttpResult.fromResult)
