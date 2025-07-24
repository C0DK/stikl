module Stikl.Web.services.Session

open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.Http

let jsonSerializerOptions =
    JsonFSharpOptions
        .Default()
        // Add any .WithXXX() calls here to customize the format
        .ToJsonSerializerOptions()

let write<'a> (session: ISession) (key: string) (value: 'a) =
    let payload = JsonSerializer.Serialize(value, jsonSerializerOptions)
    session.SetString(key, payload)

let read<'a> (session: ISession) (key: string) : 'a option =
    match session.TryGetValue(key) with
    | true, bytes -> Some(JsonSerializer.Deserialize<'a>(bytes, jsonSerializerOptions))
    | false, _ -> None

let push<'a> (session: ISession) (key: string) (value: 'a) =
    let existing = read<'a list> session key |> Option.defaultValue []
    write session key (value :: existing)

let readStack<'a> (session: ISession) (key: string) : 'a list =
    match read session key with
    | Some entries -> entries
    | None -> []
