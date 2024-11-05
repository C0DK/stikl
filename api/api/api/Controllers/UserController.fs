namespace api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open domain

type UserDto =
    { id: UserId
      needs: PlantId List
      seeds: PlantId List }

module UserDto =
    let fromDomain (user: domain.User) =
        { id = user.id
          needs = user.wants |> Set.toList
          seeds = user.seeds |> Set.toList }

    let fromDomainAsync user =
        task {
            let! user = user
            return fromDomain user
        }

module Task =
    let map func t =
        task {
            let! value = t

            return func value
        }

[<ApiController>]
[<Route("[controller]")>]
type UserController(getUsers: unit -> User List Task, getUser: UserId -> User Option Task) =
    inherit ControllerBase()

    let optionToResult o =
        match o with
        | Some value -> ObjectResult(value) :> IActionResult
        | None -> NotFoundResult() :> IActionResult


    [<HttpGet>]
    member _.GetAll() =
        getUsers () |> (Task.map (List.map UserDto.fromDomain))

    [<HttpGet("{id}")>]
    [<ProducesResponseType(typeof<domain.User>, 200)>]
    [<ProducesResponseType(typeof<string>, 404)>]
    member _.Get(id: UserId) =
        getUser id
        |> (Task.map (Option.map UserDto.fromDomain))
        |> (Task.map optionToResult)
