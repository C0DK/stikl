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
                req.plant.getAll ()
                |> Task.map (List.map Components.Common.plantCard >> Components.Common.grid >> req.pageBuilder.toPage)
                )

        // TODO: sse?
        get
            "/{id}"
            (fun
                (req:
                    {| pageBuilder: Components.Htmx.PageBuilder
                       plant: PlantRepository
                       id: string |}) ->
                req.plant.get (PlantId.parse req.id)
                |> Task.map (
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
