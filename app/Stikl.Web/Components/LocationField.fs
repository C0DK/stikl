module Stikl.Web.Components.LocationField

open Stikl.Web
open Stikl.Web.services.Location
open domain

let render (value: Location option) (locale: Localization) =
    
    //language=html
    $"""
        <div class="mb-4" id="locationPicker">
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
        $$"""
        <li>
            {{location.location.label}}
            <a class="underline {{Theme.textBrandColor}} {{Theme.textBrandColorHover}}" hx-get="/location/pick/dawa/{{location.id}}" hx-target="#locationPicker" hx-swap="outerHTML"'>{{locale.pick}}</a>
        </li>
        """
    //language=html
    $"""
    <ul class="list-inside list-disc">
        {options |> Seq.map renderChoice |> String.concat "\n"}
    </ul>
    """

