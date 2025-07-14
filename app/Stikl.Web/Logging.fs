module Stikl.Web.Logging

open Serilog
open Serilog.Events
open Serilog.Formatting.Compact

let configure () : LoggerConfiguration=

    let logLevel =
        EnvironmentVariable.get "LOG_LEVEL"
        |> Option.defaultValue "INFORMATION"
        |> fun v -> LogEventLevel.Parse(v, ignoreCase= true)
    
    LoggerConfiguration()
        .MinimumLevel.Is(logLevel)
        .WriteTo.Console(formatter=CompactJsonFormatter())