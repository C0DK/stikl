namespace Stikl.Web

open Microsoft.AspNetCore.Http
open Serilog
open Stikl.Web.Components


type HtmxErrorHandlingMiddleware(next: RequestDelegate, logger: ILogger) =
    let logger = logger.ForContext<HtmxErrorHandlingMiddleware>()

    member this.InvokeAsync(context: HttpContext) =
        task {
            try
                return! next.Invoke(context)
            with error ->
                logger.Error(error, "An unhandled exception occured")

                do!
                    (Message.error "Åh nej!" "En uventet fejl skete - prøv igen")
                    |> Results.HTML
                    |> Results.executeAsync context
        }
