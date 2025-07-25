module Result


let collect mapping result =
    match result with
    | Ok v -> mapping v
    | Error e -> Error e

let join r2 r1 =
    r1 |> collect (fun v1 -> r2 |> Result.map (fun v2 -> (v1, v2)))

let unpack r =
    match r with
    | Ok v -> v
    | Error v -> v
