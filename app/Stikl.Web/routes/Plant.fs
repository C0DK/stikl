module Stikl.Web.routes.Plant

open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Stikl.Web.Components
open Stikl.Web.Pages
open type TypedResults
open Stikl.Web
open domain

let routes =
    endpoints {
        group "p"

        get
            "/"
            (fun
                (req:
                    {| layout: Layout.Builder
                       plantCardBuilder: PlantCard.Builder
                       plant: PlantRepository |}) ->
                req.plant.getAll ()
                |> Task.map (List.map req.plantCardBuilder.render >> Common.grid >> req.layout.render))

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
                                $"No plant exists with id {ThemeGradiantSpan.render req.id}")
                    >> req.layout.render
                ))
    }
