namespace Stikl.Web

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Serilog
open Stikl.Web.Components

type HtmxErrorHandlingMiddleware(next: RequestDelegate, logger: ILogger) =
    let logger = logger.ForContext<HtmxErrorHandlingMiddleware>()

    let rec isTaskCancelled (ex: Exception) =
        match ex with
        | :? TaskCanceledException -> true
        | :? AggregateException as ex -> isTaskCancelled ex.InnerException
        | _ -> false

    member this.InvokeAsync(context: HttpContext) =
        task {
            try
                return! next.Invoke(context)
            with
            | ex when (isTaskCancelled ex) -> logger.Debug(ex, "task cancelled")
            | error when HttpRequest.IsHtmx(context) ->
                logger.Error(error, "An unhandled exception occured")

                do!
                    (Toast.errorSwap "Åh nej!" "En uventet fejl skete - prøv igen")
                    |> Results.HTML
                    |> Results.executeAsync context
        }
