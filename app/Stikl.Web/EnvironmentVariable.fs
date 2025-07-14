module Stikl.Web.EnvironmentVariable

open System

let getRequired key =
    let value = Environment.GetEnvironmentVariable key
    if String.IsNullOrWhiteSpace value then failwith $"Env var '{key}' was not set!" else value
