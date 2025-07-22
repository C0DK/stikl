namespace Stikl.Web

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
            with error when (IsHtmxRequest(context)) ->
                logger.Error(error, "An unhandled exception occured")

                do!
                    (Message.error "Åh nej!" "En uventet fejl skete - prøv igen")
                    |> Results.HTML
                    |> Results.executeAsync context
        }
