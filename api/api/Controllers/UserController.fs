namespace api.Controllers

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
        createUser: domain.UserId -> Result<unit, string> Task
    ) =
    inherit ControllerBase()



    member private this.userCreatedResult(userId: domain.UserId) =
        HttpResult.created "User" (nameof this.Get) {| id = userId.value |} userId.value

    [<HttpGet>]
    member _.GetAll() =
        getUsers () |> (Task.map (List.map Dto.User.fromDomain))

    [<HttpGet("{id}")>]
    [<ProducesResponseType(typeof<Dto.User>, 200)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    member _.Get(id: string) =
        getUser (domain.UserId id)
        |> Task.map (
            (Option.map (Dto.User.fromDomain >> HttpResult.ok))
            >> (Option.noneToNotFound $"User with id '{id}' not found")
            >> HttpError.resultToHttpResult
        )

    [<HttpPost>]
    [<ProducesResponseType(201)>]
    [<ProducesResponseType(typeof<string>, 400)>]
    [<Authorize>]
    member this.Create() =
        CurrentUser.get this
        |> Result.map (fun userId ->
            task {
                let! result = createUser userId

                return
                    match result with
                    | Ok _ -> this.userCreatedResult userId
                    // TODO how do we make the create user know that it is because it is a conflict?
                    | Error msg -> HttpResult.conflict msg
            })
        |> Task.unpackResult
        |> Task.map HttpError.resultToHttpResult
