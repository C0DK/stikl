module webapp.routes.Root

open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Stikl.Web.Pages
open type TypedResults
open webapp
open domain
open webapp.Composition
open webapp.services
open webapp.routes
open webapp.Components.Htmx


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
