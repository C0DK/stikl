module Stikl.Web.EnvironmentVariable

open System

let get key =
    let value = Environment.GetEnvironmentVariable key
    if String.IsNullOrWhiteSpace value then None else Some value

let getRequired key =
    get key
    |> Option.defaultWith (fun () -> failwith $"Env var '{key}' was not set!")
