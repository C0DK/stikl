module Stikl.Web.routes.Location

open System
open System.Threading
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Stikl.Web.Components
open Stikl.Web.services.Location
open type TypedResults
open Stikl.Web

let routes =
    endpoints {
        group "location"

        get
            "/search"
            (fun
                (req:
                    {| query: string
                       LocationService: LocationService
                       locale: Localization
                       cancellationToken: CancellationToken |}) ->
                req.LocationService.Query req.query req.cancellationToken
                |> Task.map (LocationField.renderChoices req.locale >> Results.HTML))

        get
            "/pick/dawa/{id}"
            (fun
                (req:
                    {| id: Guid
                       LocationService: LocationService
                       locale: Localization
                       cancellationToken: CancellationToken |}) ->
                // if hx-location is profile, then just update. if not then not.g

                req.LocationService.get req.id req.cancellationToken
                // TODO  not found here should throw exception.
                |> Task.map (
                    (Option.map _.location)
                    >>
                    (fun locationOption -> LocationField.render locationOption req.locale)
                    >> Results.HTML
                ))
    }
