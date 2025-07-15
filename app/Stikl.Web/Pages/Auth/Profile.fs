module Stikl.Web.Pages.Auth.Profile

open domain
open Stikl.Web.Components.Htmx

let render (user: User) =

    let keyVaule key value =
        $"<p><span class=\"font-bold text-lime-800 text-xs pr-2\">{key}</span>{value}</p>"


    $"""
    <h1 class="font-bold italic text-xl font-sans">
        Hi, {user.username}!
    </h1>
    <p>
    Her burde der nok være settings. men nah.
    </p>
    <div class="text-left">
    {keyVaule "full name" (user.fullName |> Option.defaultValue "N/A")}
    {keyVaule "firstname" (user.firstName |> Option.defaultValue "N/A")}
    {keyVaule "username" user.username}
    {keyVaule "Auth id" user.authId}
    <a
        class="transform rounded-lg border-2 px-3 py-1 border-red-900 font-sans text-sm font-bold text-red-900 transition hover:scale-105"
        href="/auth/logout"
    >
        Log Out
    </a>
    """
