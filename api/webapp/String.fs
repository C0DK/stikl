module webapp.String

let OptionFromNullOrEmpty (s: string) =
    match s with
    | null -> None
    | "" -> None
    | v-> Some v
