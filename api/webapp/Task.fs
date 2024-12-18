module webapp.Task

open System.Threading.Tasks

let map func t =
    task {
        let! value = t

        return func value
    }

let collect func t =
    task {
        let! value = t

        return! func value
    }

let unpackResult (result: Result<Task<'a>, 'b>) =
    task {
        match result with
        | Ok t ->
            let! value = t
            return Ok value
        | Error e -> return Error e

    }
