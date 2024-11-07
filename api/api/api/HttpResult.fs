module api.HttpResult

open Microsoft.AspNetCore.Mvc

let fromOption o =
    match o with
    | Some value -> OkObjectResult(value) :> IActionResult
    | None -> NotFoundResult() :> IActionResult

let badRequest (msg: string) =
    BadRequestObjectResult(msg) :> IActionResult

let notFound (msg: string) =
    NotFoundObjectResult(msg) :> IActionResult

let created controllerName actionName routeValues value =
    CreatedAtActionResult(actionName, controllerName, routeValues, value) :> IActionResult

let conflict (msg: string) =
    ConflictObjectResult(msg) :> IActionResult

let fromResult (r: Result<'a, string>) =
    match r with
    | Ok value -> OkObjectResult(value) :> IActionResult
    | Error msg -> badRequest msg

// TODO: Create a result that
