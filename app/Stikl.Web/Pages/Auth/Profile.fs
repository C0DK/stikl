module Stikl.Web.Pages.Auth.Profile

open Microsoft.AspNetCore.Antiforgery
open Stikl.Web
open domain
open Stikl.Web.Components

type Form =
    { firstName: TextField
      lastName: TextField }

    member this.isValid = this.firstName.isValid && this.lastName.isValid


let historySection (user: User) (locale: Localization) =
    let describe (e: UserEventPayload) =
        match e with
        // TODO: remove or localize
        | CreateUser _ -> "Blev oprettet"
        | AddedWant plant -> $"Ønsker sig {plant.name}"
        | AddedSeeds plantOffer -> $"Tilbyder {plantOffer.plant.name}"
        | RemovedWant plant -> $"Ønsker ikke længere {plant.name}"
        | RemovedSeeds plant -> $"Tilbyder ikke længere {plant.name}"
        | UpdateName(firstName, lastName) -> $"Opdateret navn til {firstName} {lastName}"

    let events =
        user.history
        |> Seq.map (fun e -> $"<li>{describe e}</li>")
        |> String.concat "\n"

    $"""
    <div class="{Theme.boxClasses}">
        <h3 class="{Theme.boxHeadingClasses}">{locale.history}</h3>
        <ul class="list-disc list-inside">{events}</ul>
    </div>
    """


let logOut (locale: Localization) =
    //language=html
    $"""
    <a
        class="{Theme.buttonShape} bg-red-200 border-red-900 text-red-900 mt-4 mx-auto"
        href="/auth/logout"
        hx-confirm="{locale.areYouSure}"
        hx-boost="false"
    >
       {locale.logOut} 
    </a>
    """

let fakeField label value =
    $"""
    <dt class="block text-gray-700 text-sm font-bold mb-2">
        {label}
    </dt>
    <dl class="rounded w-full py-2 px-3 text-gray-500 leading-tight mb-4">
        {value}
    </dl>
    """

let updateForm (antiForgeryToken: AntiforgeryTokenSet) (form: Form option) (user: User) (locale: Localization) =
    let createEmptyTextField (v: string) = Some(TextField.create v [])
    //language=html
    $"""
    <form
        method="post"
        class="{Theme.boxClasses}"
        >
        <h3 class="{Theme.boxHeadingClasses}">{locale.updateProfile}</h3>
        <input type="hidden" name="{antiForgeryToken.FormFieldName}" value="{antiForgeryToken.RequestToken}"/>
        <dl>
            {fakeField locale.username user.username}
        </dl>
        {TextField.render
             locale.firstName
             "firstName"
             "Pippi"
             (form
              |> Option.map _.firstName
              |> Option.orElse (createEmptyTextField user.firstName))}
        {TextField.render
             locale.lastName
             "lastName"
             "Langstrømpe"
             (form
              |> Option.map _.lastName
              |> Option.orElse (createEmptyTextField user.lastName))}
        <button 
            type="submit" 
            class="{Theme.submitButton} mx-auto" 
            >{locale.update}</button>
    </form>
    """


let render (antiForgeryToken: AntiforgeryTokenSet) (form: Form option) (user: User) =

    let locale = Localization.``default``

    $"""
    <div class="max-w-lg xl:max-w-xl grid gap-4">
        <h1 class="font-bold italic text-xl font-sans">
            {user.fullName |> locale.hi}
        </h1>
        {updateForm antiForgeryToken form user locale}
        {PickLocationForm.renderForm antiForgeryToken user.location locale}
        {historySection user locale}
        {logOut locale}
    </div>
    """
