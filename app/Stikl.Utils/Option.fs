module Option

open System.Threading.Tasks

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
