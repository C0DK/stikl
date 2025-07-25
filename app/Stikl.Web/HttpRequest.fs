module Stikl.Web.HttpRequest

open Microsoft.AspNetCore.Http

let IsHtmx (context: HttpContext) =
    match context.Request.Headers.TryGetValue("HX-Request") with
    | true, stringValues -> true
    | false, stringValues -> false
    
let IsSSE (request: HttpRequest) =
    request.Headers["Accept"].ToString().Contains("text/event-stream")
