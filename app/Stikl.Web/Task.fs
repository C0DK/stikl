module Stikl.Web.Task

open System.Threading.Tasks

let map func t =
    task {
        let! value = t

        return func value
    }

let whenAll (ts: Task seq) =
    task {
        for t in ts do
            do! t
    }

let collect func t =
    task {
        let! value = t

        return! func value
    }

let unpackOptionTask (optionTask: 'a Task option) =
    match optionTask with
    | Some t ->
        task {
            let! value = t

            return Some value
        }
    | None -> Task.FromResult None

let unpackResultTask (result: Result<Task<'a>, 'b>) =
    match result with
    | Ok t ->
        task {
            let! value = t

            return Ok value
        }
    | Error e -> Task.FromResult(Error e)



let unpackResult (result: Result<Task<'a>, 'b>) =
    task {
        match result with
        | Ok t ->
            let! value = t
            return Ok value
        | Error e -> return Error e

    }

let unpackOption (option: 'a option Task Option) =
    match option with
    | Some t ->
        task {
            let! value = t
            return value

        }
    | None -> Task.FromResult None

let merge (ts: 'a Task seq) : 'a list Task = ts |> Task.WhenAll |> map Seq.toList
