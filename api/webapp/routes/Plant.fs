module webapp.routes.Plant

open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open type TypedResults
open webapp.Composition
open webapp
open domain

let routes =
    endpoints {
        group "plant"

        get
            "/"
            (fun
                (req:
                    {| pageBuilder: Components.Htmx.PageBuilder
                       plant: PlantRepository |}) ->
                task {
                    let! plants = req.plant.getAll ()
                    let cards = plants |> List.map Components.Common.plantCard

                    return! req.pageBuilder.toPage (Components.Common.grid cards)
                })

        // TODO: sse?
        get
            "/{id}"
            (fun
                (req:
                    {| pageBuilder: Components.Htmx.PageBuilder
                       plant: PlantRepository
                       id: string |}) ->
                req.plant.get (PlantId.parse req.id)
                |> Task.collect (
                    (fun plant ->
                        match plant with
                        | Some plant -> Pages.Plant.Details.render plant
                        | None ->
                            Pages.NotFound.render
                                "Plant not found!"
                                $"No plant exists with id {Components.Common.themeGradiantSpan req.id}")
                    >> req.pageBuilder.toPage
                ))
    }
