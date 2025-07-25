module Stikl.Web.routes.Root

open System.Threading
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Stikl.Web
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
        Chat.routes
        Location.routes

        get "/" (fun (req: {| layout: Layout.Builder |}) -> Index.render req.layout)

    }

let apply = routes.Apply
