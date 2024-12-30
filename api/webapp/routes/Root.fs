module webapp.routes.Root

open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http

open System.Threading.Tasks
open FSharp.MinimalApi.Builder
open type TypedResults
open webapp
open domain
open webapp.services
open webapp.routes

let toPlantCards l =
    l |> List.map Components.plantCard |> String.concat "\n"


let routes =
    endpoints {
        Auth.routes
        Plant.routes
        User.routes
        Trigger.routes

        // TODO: use pageBuilder on all endpoints.
        get "/" (fun (req: {| renderPage: Page.PageBuilder |}) ->
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
                       httpContext: HttpContext
                       users: User.UserSource |}) ->
                task {
                    let query = req.query.ToLower()

                    // TODO: cache user somewhere.. maybe DI?
                    let! user =
                        req.principal
                        |> Option.map (fun p -> req.users.getUserById p.auth0Id)
                        |> Option.defaultValue (Task.FromResult None)

                    let likedAndToken plant =
                        user
                        |> Option.map (fun user -> user.wants |> Seq.exists (fun p -> p.id = plant.id))
                        |> Option.map (fun l -> (l, req.antiForgery.GetAndStoreTokens(req.httpContext)))

                    let plantCards =
                        Composition.plants
                        |> List.filter (_.name.ToLower().Contains(query))
                        |> List.map (fun p -> (Components.authedPlantCard (likedAndToken p) p))

                    let! users = req.users.query query

                    let userCards = users |> List.map Components.identityCard


                    return Page.toOkResult ((plantCards @ userCards) |> String.concat "\n")
                })

    }

let apply = routes.Apply
