module webapp.routes.Root

open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open type TypedResults
open webapp
open domain
open webapp.Page
open webapp.routes

let toPlantCards l =
    l |> List.map Components.plantCard |> String.concat "\n"


let routes =
    endpoints {
        Auth.routes
        Plant.routes

        // TODO: use pageBuilder on all endpoints.
        get "/" (fun (req: {| pageBuilder: PageBuilder |}) ->
            let stiklingerFrøOgPlanter =
                Components.themeGradiantSpan "Stiklinger, frø og planter"

            let title = Components.PageHeader $"Find {stiklingerFrøOgPlanter} nær dig"

            let callToAction =
                """
<p class="mb-8 max-w-md text-center text-lg md:text-xl">
    Deltag i et fælleskab hvor vi gratis deler frø, planer og stiklinger. At undgå industrielt voksede planter er ikke bare billigere for dig - men også for miljøet.
</p>
"""

            req.pageBuilder.ToPage(title + callToAction + Components.search))

        get "/search" (fun (req: {| query: string |}) ->
            let plantCards =
                Composition.plants
                |> List.filter (_.name.ToLower().Contains(req.query.ToLower()))
                |> toPlantCards

            toOkResult plantCards)

    }

let apply = routes.Apply
