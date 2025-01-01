module Option

let orFail option =
    option |> Option.defaultWith (fun () -> failwith "Unexpected None!")
