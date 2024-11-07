module api.Result

let collect f r =
    match r with
    | Ok v -> f v
    | Error e -> Error e

let unpack (r: Result<Result<'a, 'b>, 'b>) =
    match r with
    | Ok v -> v
    | Error e -> Error e
