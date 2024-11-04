namespace api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open domain

[<ApiController>]
[<Route("[controller]")>]
type UserController (getUsers: unit -> User List Task, getUser: UserId -> User Option Task) =
    inherit ControllerBase()

    [<HttpGet>]
    member _.GetAll()= getUsers()

    [<HttpGet("{id}")>]
    member _.Get(id: UserId) = getUser id
