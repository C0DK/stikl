module api.HttpResult

open Microsoft.AspNetCore.Mvc

let fromOption o =
    match o with
    | Some value -> ObjectResult(value) :> IActionResult
    | None -> NotFoundResult() :> IActionResult

let fromResult (r: Result<'a, string>) =
    match r with
    | Ok value -> ObjectResult(value) :> IActionResult
    | Error msg -> BadRequestObjectResult(msg) :> IActionResult
