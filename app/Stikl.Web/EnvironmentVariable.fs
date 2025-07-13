module Stikl.Web.EnvironmentVariable

open System

let getRequired key =
    // TODO does this fail if empty?
    match Environment.GetEnvironmentVariable key with
    | v when v.Trim() <> "" -> v
    | _ -> failwith $"Env var '{key}' was not set!"
