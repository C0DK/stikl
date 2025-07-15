module Stikl.Web.routes.Root

open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Stikl.Web.Pages
open type TypedResults
open Stikl.Web
open domain
open Stikl.Web.Composition
open Stikl.Web.services
open Stikl.Web.routes
open Stikl.Web.Components.Htmx


let routes =
    endpoints {
        Auth.routes
        Plant.routes
        User.routes
        Trigger.routes
        Search.routes

        get "/" (fun (req: {| layout: Layout.Builder |}) -> Pages.Index.render req.layout)

    }

let apply = routes.Apply
