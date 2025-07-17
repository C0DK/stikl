module Stikl.Web.Pages.Auth.Create

open Microsoft.AspNetCore.Antiforgery
open Stikl.Web
open Stikl.Web.Components


type Form =
    { username: TextField
      firstName: TextField
      lastName: TextField }

    member this.isValid =
        this.firstName.isValid && this.lastName.isValid && this.username.isValid


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
        {TextField.render "Brugernavn" "username" "Indsæt et unikt brugernavn" (form |> Option.map _.username)}
        {TextField.render "Fornavn" "firstName" "Pippi" (form |> Option.map _.firstName)}
        {TextField.render "Efternavn" "lastName" "Langstrømpe" (form |> Option.map _.lastName)}
        <button 
            type="submit" 
            class="{Theme.submitButton} mx-auto " 
            >Opret</button>
    </form>
    """
