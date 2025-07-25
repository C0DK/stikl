module Option

open System.Threading.Tasks
open Microsoft.AspNetCore.Http

let or404NotFound (option: Option<IResult>) =
    option |> Option.defaultValue (Results.NotFound())

let or404NotFoundTask (option: Option<Task<IResult>>) =
    option |> Option.defaultValue (Results.NotFound() |> Task.FromResult)
