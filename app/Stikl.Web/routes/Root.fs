module Stikl.Web.routes.Root

open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Stikl.Web.Pages
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

    }

let apply = routes.Apply
