namespace Stikl.Web.Components

open System

open System.Threading.Tasks
open Stikl.Web

type TextField =
    { value: string
      errors: string array }

    member this.isValid = this.errors.Length = 0

module TextField =
    let render (label: string) (id: string) (placeholder: string) (field: TextField option) =
        let errors =
            field
            |> Option.bind (fun field ->
                match field.errors with
                | [||] -> None
                | _ ->
                    Some(
                        "<ul class=\"list-disc list-inside text-red-600 text-sm\">"
                        + (field.errors |> Seq.map (fun e -> $"<li>{e}</li>") |> String.concat "\n")
                        + "</ul>"
                    ))
            |> Option.defaultValue ""

        $"""
        <div class="mb-4">
            <label class="block text-gray-700 text-sm font-bold mb-2" for="{id}">
                {label}
            </label>
            <input
                class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline"
                id="{id}"
                name="{id}"
                value="{field |> Option.map _.value |> Option.defaultValue ""}"
                type="text" placeholder="{placeholder}"
            >
            {errors}
        </div>
        """

    let validateNonEmpty v =
        if String.IsNullOrWhiteSpace v then
            [ "Dette felt skal udfyldes" ]
        else
            []

    let validateAlphaNumericUnderscores v =
        if v |> Seq.exists ((fun c -> Char.IsLetterOrDigit c && Char.IsLower c) >> not) then
            [ "Må kun indeholde små bogstaver og tal" ]
        else
            []

    let create (value: string) (validators: (string -> string list) seq) =
        { value = value
          errors = validators |> Seq.collect (fun validator -> (validator value)) |> Seq.toArray }

    let createAsync (value: string) (validators: (string -> string list Task) seq) =
        validators
        |> Seq.map (fun validator -> validator value)
        |> Task.merge
        |> Task.map (
            Seq.collect id
            >> Seq.toArray
            >> fun errors -> { value = value; errors = errors }
        )
