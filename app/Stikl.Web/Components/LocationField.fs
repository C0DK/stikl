namespace Stikl.Web.Components

open Stikl.Web
open domain

type LocationField =
    { value: DawaLocation option
      errors: string array }

    member this.isValid = this.errors.Length = 0
module LocationField =
    let create(value: Result<DawaLocation, string>) =
        match value with
            | Ok location -> {value= Some location; errors= Array.empty}
            | Error error -> {value= None; errors= [|error|]}
        
    
    let private baseField (locale: Localization) (field: string) =
        $"""
            <div class="mb-4" id="locationPicker">
                <label class="block text-gray-700 text-sm font-bold mb-2" for="{id}">
                {locale.location}
                </label>
                {field}
            </div>
        """

    let render (locale: Localization) (value: DawaLocation option) =
        let hiddenInput =
            match value with
            | Some location -> $"<input type=\"hidden\" name=\"location\" value=\"{location.id}\"/>"
            | None -> ""
        baseField
            locale
            $"""
            <div class="shadow rounded w-full text-gray-500 leading-tight mb-4 flex">
                <span class="w-2/3 py-2 px-3">
                    {value |> Option.map _.location.label |> Option.defaultValue locale.setLocation}
                </span>
                <button class="w-1/3 {Theme.buttonShape} rounded-l-none" hx-get="/location/search" hx-swap="outerHTML" hx-target="#locationPicker">{locale.edit}</button>
            </div>
            {hiddenInput}
            """

    let renderSearch (locale: Localization) (value: DawaLocation option) =
        //language=html
        baseField
            locale
            $"""
            <input
                class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline"
                name="query"
                autofocus
                hx-get="/location/search/results"
                hx-trigger="search, keyup delay:200ms changed"
                hx-target="#locationOptions"
                value="{value |> Option.map _.location.label |> Option.defaultValue ""}"
                type="text" placeholder="{locale.searchForLocation}"
            >
            <ul id="locationOptions" class="list-inside list-disc">
            </ul>
            """

    let renderChoices (locale: Localization) (options: DawaLocation list) =
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
