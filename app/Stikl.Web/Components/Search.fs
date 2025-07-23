module Stikl.Web.Components.Search

open Stikl.Web
open domain

module Results =
    // TODO the PlantCardBuilder here doesnt refresh the user state in an SSE stream. fix!
    let render (plants: Plant seq) (users: User seq) (plantCardBuilder: Plant -> string) =
        let plantCards = plants |> Seq.map plantCardBuilder

        let userCards = users |> Seq.map UserCard.render

        plantCards |> Seq.append userCards |> CardGrid.render

module Form =
    let render =
        let locale = Localization.``default``
        // language=html
        $"""
        <search>
            <input
               class="bg-gray-50 border border-gray-300 text-gray-900 appearance-none text-sm {Theme.rounding} focus:border-lime-500 focus:ring-transparent block p-2.5"
               type="search"
               name="query" placeholder="{locale.search}"
               hx-get="/search"
               hx-trigger="input changed delay:500ms, keyup[key=='Enter']"
               hx-target="#search-results"
               >
        </search>
        <div id="search-results">
        </div>
        """
