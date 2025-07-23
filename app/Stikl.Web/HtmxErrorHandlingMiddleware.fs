namespace Stikl.Web

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Serilog
open Stikl.Web.Components

type HtmxErrorHandlingMiddleware(next: RequestDelegate, logger: ILogger) =
    let logger = logger.ForContext<HtmxErrorHandlingMiddleware>()
    
    let IsHtmxRequest (context: HttpContext) =
        match context.Request.Headers.TryGetValue("HX-Request") with
        | true, stringValues ->true  
        | false, stringValues -> false


    member this.InvokeAsync(context: HttpContext) =
        task {
            try
                return! next.Invoke(context)
            with
            | :? AggregateException as ex when (ex.InnerException :? TaskCanceledException) ->
                logger.Debug(ex, "task cancelled")
            | :? TaskCanceledException as ex ->
                logger.Debug(ex, "task cancelled")
            | error when IsHtmxRequest(context) ->
                logger.Error(error, "An unhandled exception occured")

                do!
                    (Message.error "Åh nej!" "En uventet fejl skete - prøv igen")
                    |> Results.HTML
                    |> Results.executeAsync context
        }
