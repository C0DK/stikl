module Stikl.Web.routes.Location

open System
open System.Threading
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Stikl.Web.Components
open Stikl.Web.services.Location
open Stikl.Web.services.User
open type TypedResults
open Stikl.Web

let routes =
    endpoints {
        group "location"

        get
            "/search/results"
            (fun
                (req:
                    {| query: string
                       LocationService: LocationService
                       locale: Localization
                       cancellationToken: CancellationToken |}) ->
                req.LocationService.Query req.query req.cancellationToken
                |> Task.map (LocationField.renderChoices req.locale >> Results.HTML))

        get
            "/search"
            (fun
                (req:
                    {| LocationService: LocationService
                       locale: Localization
                       identity: CurrentUser
                       cancellationToken: CancellationToken |}) ->
                req.identity.get
                |> Option.map _.location
                |> LocationField.renderSearch req.locale
                |> Results.HTML)

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
                    Result.map (Some >> LocationField.render req.locale >> Results.HTML)
                    >> Toast.errorToResult
                ))
    }
