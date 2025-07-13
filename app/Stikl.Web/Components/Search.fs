module webapp.Components.Search

open domain
open webapp
open webapp.Components.Htmx

module Results =
    let render (plants: Plant seq) (users: User seq) (pageBuilder: PageBuilder) =
        let plantCards = plants |> Seq.map pageBuilder.plantCard

        let userCards = users |> Seq.map pageBuilder.userCard

        $"""
         <div class="grid sm:grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {plantCards |> Seq.append userCards |> String.concat "\n"}
         </div>
        """

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
