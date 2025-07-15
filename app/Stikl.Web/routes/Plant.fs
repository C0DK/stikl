module Stikl.Web.routes.Plant

open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Stikl.Web.Pages
open type TypedResults
open Stikl.Web.Composition
open Stikl.Web
open domain

let routes =
    endpoints {
        group "plant"

        get
            "/"
            (fun
                (req:
                    {| layout: Layout.Builder
                       plant: PlantRepository |}) ->
                req.plant.getAll ()
                |> Task.map (
                    List.map Components.Common.plantCard
                    >> Components.Common.grid
                    >> req.layout.render
                ))

        // TODO: sse?
        get
            "/{id}"
            (fun
                (req:
                    {| layout: Layout.Builder
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
                    >> req.layout.render
                ))
    }
