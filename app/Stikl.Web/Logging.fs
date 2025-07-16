module Stikl.Web.Logging

open Serilog
open Serilog.Events
open Serilog.Formatting.Compact

let configure () : LoggerConfiguration =

    let logLevel =
        EnvironmentVariable.get "LOG_LEVEL"
        |> Option.defaultValue "INFORMATION"
        |> fun v -> LogEventLevel.Parse(v, ignoreCase = true)

    LoggerConfiguration()
        .MinimumLevel.Is(logLevel)
        .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore.Http.Result.ContentResult", LogEventLevel.Warning)
        // TODO: use prettier variant locally.
        .WriteTo.Console(formatter = RenderedCompactJsonFormatter())
