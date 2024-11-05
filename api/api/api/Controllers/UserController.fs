namespace api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open domain

open api


[<ApiController>]
[<Route("[controller]")>]
type UserController(getUsers: unit -> User List Task, getUser: UserId -> User Option Task) =
    inherit ControllerBase()



    [<HttpGet>]
    member _.GetAll() =
        getUsers () |> (Task.map (List.map Dto.User.fromDomain))

    [<HttpGet("{id}")>]
    [<ProducesResponseType(typeof<domain.User>, 200)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    member _.Get(id: UserId) =
        getUser id
        |> (Task.map (Option.map Dto.User.fromDomain))
        |> (Task.map HttpResult.fromOption)
