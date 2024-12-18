namespace webapp

#nowarn "20"

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.HttpsPolicy
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

open domain

module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddControllers()

        let app = builder.Build()

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()

        app.MapGet(
            "/",
            Func<IResult>(fun () ->
                let stiklingerFrøOgPlanter = Htmx.themeGradiantSpan "Stiklinger, frø og planter"
                let title = Htmx.PageHeader $"Find {stiklingerFrøOgPlanter} nær dig"

                let callToAction =
                    (Htmx.p
                        "mb-8 max-w-md text-center text-lg md:text-xl"
                        "Deltag i et fælleskab hvor vi gratis deler frø, planer og stiklinger. At undgå industrielt voksede planter er ikke bare billigere for dig - men også for miljøet.")

                let search =
                    """
<span class="htmx-indicator">
    Searching...
</span>
<input class="form-control" type="search"
   name="query" placeholder="Begin Typing To Search Users..."
   hx-get="/search"
   hx-trigger="input changed delay:500ms, keyup[key=='Enter']"
   hx-target="#search-results"
   hx-indicator=".htmx-indicator">

<div id="search-results" class="grid grid-cols-3 gap-4">
</div>
"""


                Htmx.page (title + callToAction + search))
        )

        let toPlantCards l =
            l |> List.map Htmx.plantCard |> String.concat "\n"

        app.MapGet(
            "/search",
            Func<string, IResult>(fun query ->
                let plantCards =
                    Composition.plants
                    |> List.filter (_.name.ToLower().Contains(query.ToLower()))
                    |> toPlantCards

                Htmx.toResult plantCards)
        )

        app.MapGet(
            "/plant",
            Func<IResult>(fun () ->
                let plantCards = Composition.plants |> toPlantCards

                Htmx.page (Htmx.grid plantCards))
        )

        app.MapGet(
            "/plant/{id}",
            Func<string, IResult>(fun id ->
                let plantOption = Composition.plants |> List.tryFind (fun p -> p.id.ToString() = id)

                (match plantOption with
                 | Some plant -> Htmx.page ((Htmx.PageHeader plant.name) + (Htmx.p "" "Det er en ret flot plante!"))
                 | None ->
                     Htmx.page (
                         (Htmx.PageHeader "Plant not found!")
                         + (Htmx.p "" $"No plant exists with id '{id}'")
                     )))
        )



        app.Run()

        exitCode
