namespace api

open Microsoft.AspNetCore.Mvc

type HttpError =
    | NotFound of string
    | BadRequest of string
    | Conflict of string

type HttpErrorPayload = { message: string; code: int }

module HttpError =
    let toHttpResult error =
        match error with
        | NotFound msg -> NotFoundObjectResult({ message = msg; code = 404 }) :> IActionResult
        | Conflict msg -> ConflictObjectResult({ message = msg; code = 409 }) :> IActionResult
        | BadRequest msg -> BadRequestObjectResult({ message = msg; code = 400 }) :> IActionResult

    let resultToHttpResult (r: Result<IActionResult, HttpError>) =
        match r with
        | Ok value -> value
        | Error error -> toHttpResult error

module HttpResult =
    let ok elm = OkObjectResult(elm) :> IActionResult

    let notFoundFromOption msg o =
        match o with
        | Some value -> Ok(value)
        | None -> Error(NotFound msg)

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



module Option =
    let noneToNotFound msg o =
        match o with
        | Some value -> Ok(value)
        | None -> Error(HttpError.NotFound msg)
