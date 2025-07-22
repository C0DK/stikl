module Stikl.Web.Pages.Auth.Create

open Microsoft.AspNetCore.Antiforgery
open Stikl.Web
open Stikl.Web.Components


type Form =
    { username: TextField
      firstName: TextField
      lastName: TextField
      location: LocationField
       }

    member this.isValid =
        this.firstName.isValid && this.lastName.isValid && this.username.isValid && this.location.isValid


let render (antiForgeryToken: AntiforgeryTokenSet) (form: Form option) =
    let locale = Localization.``default``
    let locationErrors =
        match form with
        | Some form when form.location.errors.Length > 0 -> TextField.errorList form.location.errors
        | _ -> ""
    $"""
    <div class="{Theme.boxClasses}">
        <h1 class="{Theme.boxHeadingClasses}">
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
            {LocationField.renderSearch locale (form |> Option.bind _.location.value)}
            {locationErrors}
            <button 
                type="submit" 
                class="{Theme.submitButton} mx-auto " 
                >Opret</button>
        </form>
    </div>
    """
