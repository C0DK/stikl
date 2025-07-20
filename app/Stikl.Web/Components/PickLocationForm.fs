module Stikl.Web.Components.PickLocationForm

open Microsoft.AspNetCore.Antiforgery
open Stikl.Web
open Stikl.Web.services.Location
open domain

let renderForm (antiForgeryToken: AntiforgeryTokenSet) (value: Location option) (locale: Localization) =
    
    //language=html
    $"""

        <div class="{Theme.boxClasses}">
            <label class="block text-gray-700 text-sm font-bold mb-2" for="{id}">
            {locale.setLocation}
            </label>
            <input
                class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline"
                name="query"
                hx-get="/location/search"
                hx-trigger="search, keyup delay:200ms changed"
                hx-target="#locationOptions"
                value="{value |> Option.map _.label |> Option.defaultValue ""}"
                type="text" placeholder="{locale.searchForLocation}"
            >
            <ul id="locationOptions" class="list-inside list-disc">
            </ul>
        </div>
    """

let renderChoices (locale: Localization) (options: DawaLocation list) =
    // TODO find better value to pass
    let renderChoice (location: DawaLocation) =
        //language=html
        $"""
        <li>
            {location.location.label}
            <button class="{Theme.smButton}" value="{location.id}">{locale.pick}</button>
        </li>
        """
    //language=html
    $"""
    <ul class="list-inside list-disc">
        {options |> Seq.map renderChoice |> String.concat "\n"}
    </ul>
    """

