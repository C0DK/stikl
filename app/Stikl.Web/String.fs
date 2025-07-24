module Stikl.Web.String

open System.Web

let OptionFromNullOrEmpty (s: string) =
    match s with
    | null -> None
    | "" -> None
    | v -> Some v

let escape (s: string) = s |> HttpUtility.HtmlEncode
