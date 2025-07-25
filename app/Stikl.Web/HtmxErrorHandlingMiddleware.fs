namespace Stikl.Web

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Serilog
open Stikl.Web.Components

type HtmxErrorHandlingMiddleware(next: RequestDelegate, logger: ILogger) =
    let logger = logger.ForContext<HtmxErrorHandlingMiddleware>()


    member this.InvokeAsync(context: HttpContext) =
        task {
            try
                return! next.Invoke(context)
            with
            // TODO: seems to sometimes be infinitely nested??.. :7
            | :? AggregateException as ex when (ex.InnerException :? TaskCanceledException) ->
                logger.Debug(ex, "task cancelled")
            | :? TaskCanceledException as ex -> logger.Debug(ex, "task cancelled")
            | error when HttpRequest.IsHtmx(context) ->
                logger.Error(error, "An unhandled exception occured")

                do!
                    (Toast.errorSwap "Åh nej!" "En uventet fejl skete - prøv igen")
                    |> Results.HTML
                    |> Results.executeAsync context
        }
