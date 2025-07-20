module Stikl.Web.routes.Root

open System.Threading
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Stikl.Web
open Stikl.Web.Components
open Stikl.Web.Pages
open Stikl.Web.services.Location
open type TypedResults
open Stikl.Web.routes


let routes =
    endpoints {
        Auth.routes
        Plant.routes
        User.routes
        Trigger.routes
        Search.routes

        get "/" (fun (req: {| layout: Layout.Builder |}) -> Index.render req.layout)
        get "/location/search" (fun (req: {| query: string; LocationService: LocationService; locale: Localization; cancellationToken: CancellationToken |}) ->
            req.LocationService.Query req.query req.cancellationToken
            |> Task.map (PickLocationForm.renderChoices req.locale >> Results.HTML)
            )

    }

let apply = routes.Apply
