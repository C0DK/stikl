module Stikl.Web.Logging

open System
open Microsoft.Extensions.Logging
open Serilog
open Serilog.Sinks.Grafana.Loki

let configure () =

    let logLevel =
        EnvironmentVariable.get "LOG_LEVEL"
        |> Option.defaultValue "INFO"
        |> fun v -> LogLevel.Parse(v, ignoreCase= true)
    
    let config =
        LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .WriteTo.Console()
            
    match EnvironmentVariable.get "LOKI_URL" with
    | Some lokiUrl -> config.WriteTo.GrafanaLoki(lokiUrl, labels= [LokiLabel(Key="app",Value="Stikl.Web")])
    | None -> config
