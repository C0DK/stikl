module webapp.routes.Root

open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http

open System.Threading.Tasks
open FSharp.MinimalApi.Builder
open type TypedResults
open webapp
open domain
open webapp.Composition
open webapp.services
open webapp.routes
open webapp.services.Htmx

let toPlantCards l =
    l |> List.map Components.plantCard |> String.concat "\n"


let routes =
    endpoints {
        Auth.routes
        Plant.routes
        User.routes
        Trigger.routes

        // TODO: use pageBuilder on all endpoints.
        get "/" (fun (req: {| renderPage: Htmx.PageBuilder |}) ->
            let stiklingerFrøOgPlanter =
                Components.themeGradiantSpan "Stiklinger, frø og planter"

            let title = Components.PageHeader $"Find {stiklingerFrøOgPlanter} nær dig"

            let callToAction =
                """
<p class="mb-8 max-w-md text-center text-lg md:text-xl">
    Deltag i et fælleskab hvor vi gratis deler frø, planer og stiklinger. At undgå industrielt voksede planter er ikke bare billigere for dig - men også for miljøet.
</p>
"""

            req.renderPage.toPage (title + callToAction + Components.search))

        get
            "/search"
            (fun
                (req:
                    {| query: string
                       principal: Principal option
                       antiForgery: IAntiforgery
                       plants: PlantRepository
                       pageBuilder: PageBuilder
                       httpContext: HttpContext
                       users: User.UserSource |}) ->
                task {
                    let query = req.query.ToLower()

                    // TODO use PlantRepo
                    let! plantCards =
                        plants
                        |> List.filter (_.name.ToLower().Contains(query))
                        |> List.map req.pageBuilder.plantCard
                        |> Task.combine

                    let! users = req.users.query query

                    let userCards = users |> List.map Components.identityCard

                    return Result.Html.Ok((plantCards @ userCards) |> String.concat "\n")
                })

    }

let apply = routes.Apply
