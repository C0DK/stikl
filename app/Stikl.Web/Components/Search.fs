module Stikl.Web.Components.Search

open domain
open Stikl.Web.Components.Htmx

module Results =
    // TODO the pagebuilder here doesnt refresh the user state in an SSE stream. fix!
    let render (plants: Plant seq) (users: User seq) (pageBuilder: PageBuilder) =
        let plantCards = plants |> Seq.map pageBuilder.plantCard

        let userCards = users |> Seq.map pageBuilder.userCard

        plantCards |> Seq.append userCards |> CardGrid.render

module Form =
    let render =
        // language=html
        """
        <search>
            <input
               class="bg-gray-50 border border-gray-300 text-gray-900 appearance-none text-sm rounded-lg focus:border-lime-500 focus:ring-transparent block p-2.5"
               type="search"
               name="query" placeholder="Søg efter planter eller personer..."
               hx-get="/search"
               hx-trigger="input changed delay:500ms, keyup[key=='Enter']"
               hx-target="#search-results"
               hx-indicator=".htmx-indicator">
        </search>
        <span class="font-bold text-2xl pulse htmx-indicator">
            ...
        </span>

        <div id="search-results">
        </div>
        """
