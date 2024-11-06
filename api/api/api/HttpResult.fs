module api.HttpResult

open Microsoft.AspNetCore.Mvc

let fromOption o =
    match o with
    | Some value -> OkObjectResult(value) :> IActionResult
    | None -> NotFoundResult() :> IActionResult

let badRequest (msg: string) =
    BadRequestObjectResult(msg) :> IActionResult

let created = CreatedResult() :> IActionResult

let conflict (msg: string) =
    ConflictObjectResult(msg) :> IActionResult
// TODO: Handle created etc
let fromResult (r: Result<'a, string>) =
    match r with
    | Ok value -> OkObjectResult(value) :> IActionResult
    | Error msg -> badRequest (msg)
