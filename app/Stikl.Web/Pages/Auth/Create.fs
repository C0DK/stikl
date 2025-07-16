module Stikl.Web.Pages.Auth.Create

open System.Threading.Tasks
open Microsoft.AspNetCore.Antiforgery
open Stikl.Web
open FSharp.Control

type ErrorList = string array

type Field =
    { value: string
      errors: ErrorList }

    member this.isValid = this.errors.Length = 0

    static member create (value: string) (validators: (string -> string list) seq) =
        { value = value
          errors = validators |> Seq.collect (fun validator -> (validator value)) |> Seq.toArray }

    static member createAsync (value: string) (validators: (string -> string list Task) seq) =
        validators
        |> Seq.map (fun validator -> validator value)
        |> Task.merge
        |> Task.map (
            Seq.collect id
            >> Seq.toArray
            >> fun errors -> { value = value; errors = errors }
        )

type Form =
    { username: Field
      firstName: Field
      lastName: Field }

    member this.isValid =
        this.firstName.isValid && this.lastName.isValid && this.username.isValid

let textInput (label: string) (id: string) (placeholder: string) (field: Field option) =
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

let render (antiForgeryToken: AntiforgeryTokenSet) (form: Form option) =
    $"""
    <h1 class="font-bold italic text-xl font-sans">
        Opret bruger
    </h1>
        <form
            method="post"
            action="/auth/create"
            class="p-4 grid"
            >
            <input type="hidden" name="{antiForgeryToken.FormFieldName}" value="{antiForgeryToken.RequestToken}"/>
            {textInput "Brugernavn" "username" "Indsæt et unikt brugernavn" (form |> Option.map _.username)}
            {textInput "Fornavn" "firstName" "Pippi" (form |> Option.map _.firstName)}
            {textInput "Efternavn" "lastName" "Langstrømpe" (form |> Option.map _.lastName)}
            <button 
                type="submit" 
                class="{Theme.submitButton} mx-auto " 
                >Opret</button>
        </form>
    """
