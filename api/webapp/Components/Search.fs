module webapp.Components.Search

open domain
open webapp
open webapp.Components.Htmx

module Results =
    let render (plants: Plant seq) (users: User seq) (pageBuilder: PageBuilder) =
        let plantCards = plants |> Seq.map pageBuilder.plantCard

        let userCards = users |> Seq.map pageBuilder.userCard

        plantCards |> Seq.append userCards |> String.concat "\n"

module Form =
    let render =
        // language=html
        """
        <input
           class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:border-lime-500 focus:ring-transparent block p-2.5"
           type="search"
           name="query" placeholder="Søg efter planter eller personer..."
           hx-get="/search"
           hx-trigger="input changed delay:500ms, keyup[key=='Enter']"
           hx-target="#search-results"
           hx-indicator=".htmx-indicator">
           
        <span class="font-bold text-2xl pulse htmx-indicator">
            ...
        </span>

        <div id="search-results" class="grid grid-cols-3 gap-4">
        </div>
        """
