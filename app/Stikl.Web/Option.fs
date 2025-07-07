module Option

open System.Threading.Tasks
open Microsoft.AspNetCore.Http

let orFail option =
    option |> Option.defaultWith (fun () -> failwith "Unexpected None!")


let bindTask (f: 'a -> 'b option Task) (o: 'a option) =
    match o with
    | Some v ->
        task {
            let! value = f v
            return value

        }
    | None -> Task.FromResult None


let or404NotFound (option: Option<IResult>) =
    option |> Option.defaultValue (Results.NotFound())

let or404NotFoundTask (option: Option<Task<IResult>>) =
    option |> Option.defaultValue (Results.NotFound() |> Task.FromResult)
