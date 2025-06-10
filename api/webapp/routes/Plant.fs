module webapp.routes.Plant

open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open type TypedResults
open webapp
open webapp.Composition
open webapp.services
open domain

let routes =
    endpoints {
        group "plant"

        get
            "/"
            (fun
                (req:
                    {| renderPage: Htmx.PageBuilder
                       plant: PlantRepository |}) ->
                task {
                    let! plants = req.plant.getAll ()
                    let cards = plants |> List.map Components.plantCard

                    return! req.renderPage.toPage (Components.grid cards)
                })

        get
            "/{id}"
            (fun
                (req:
                    {| renderPage: Htmx.PageBuilder
                       plant: PlantRepository
                       id: string |}) ->
                task {
                    let! plant = req.plant.get (PlantId.parse req.id)

                    return!
                        req.renderPage.toPage (
                            match plant with
                            | Some plant ->
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
                                ((Components.PageHeader "Plant not found!")
                                 + $"""
        <p class="text-center text-lg md:text-xl">
          No plant exists with id {Components.themeGradiantSpan req.id}
        </p>
        """
                                 + Components.search)
                        )
                })


    }
