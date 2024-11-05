module api.HttpResult

open Microsoft.AspNetCore.Mvc

let fromOption o =
    match o with
    | Some value -> ObjectResult(value) :> IActionResult
    | None -> NotFoundResult() :> IActionResult
