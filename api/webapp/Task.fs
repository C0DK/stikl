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
    
let unpackOptionTask (optionTask : 'a Task option)=
    match optionTask with
    | Some t -> task {
        let! value = t
        
        return Some value
        }
    | None -> Task.FromResult None
    
let unpackResultTask (result : Result<Task<'a>,'b>)=
    match result with
    | Ok t -> task {
        let! value = t
        
        return Ok value
        }
    | Error e -> Task.FromResult (Error e)
    
     

let combine (t: 'a Task seq) =
    task {
        let! o = t |> Task.WhenAll

        return o |> Seq.toList

    }

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
