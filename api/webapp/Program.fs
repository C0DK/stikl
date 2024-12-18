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


        app.MapGet(
            "/",
            Func<IResult>(fun () ->
                let stiklingerFrøOgPlanter = Htmx.themeGradiantSpan "Stiklinger, frø og planter"
                let title = Htmx.PageHeader $"Find {stiklingerFrøOgPlanter} nær dig"

                let callToAction =
                    (Htmx.p
                        "mb-8 max-w-md text-center text-lg md:text-xl"
                        "Deltag i et fælleskab hvor vi gratis deler frø, planer og stiklinger. At undgå industrielt voksede planter er ikke bare billigere for dig - men også for miljøet.")



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
                 | Some plant ->
                     Htmx.page
                         $"""
                         
<div class="flex w-full justify-between pl-10 pt-5">
	<div class="flex">
		<div class="mr-5">
			<img
				alt="Image of a {plant.name}"
				class="h-32 w-32 rounded-full object-cover"
				src={plant.image_url}
			/>
		</div>
		<div class="content-center">
			<h1 class="font-sans text-3xl font-bold text-lime-800">{plant.name}</h1>
			<p class="max-w-72 pl-2 text-sm font-bold text-slate-600">
   TODO beskrivelse og tags?
			</p>
		</div>
	</div>
 </div>
"""
                 | None ->
                     Htmx.page (
                         (Htmx.PageHeader "Plant not found!")
                         + (Htmx.p
                             "text-center text-lg md:text-xl"
                             $"No plant exists with id {Htmx.themeGradiantSpan id}")
                         + search
                     )))
        )



        app.Run()

        exitCode
