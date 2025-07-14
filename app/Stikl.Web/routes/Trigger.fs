module webapp.routes.Trigger

open System

open System.Threading.Tasks
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Mvc
open Stikl.Web.Pages
open webapp
open webapp.Components
open webapp.services
open webapp.Composition
open domain



type PlantEventParams =
    { pageBuilder: Htmx.PageBuilder
      [<FromForm>]
      plantId: string
      eventHandler: EventHandler
      context: HttpContext
      plantRepository: PlantRepository }

type AddSeedsParams =
    { pageBuilder: Htmx.PageBuilder
      layout: Layout.Builder
      [<FromForm>]
      plantId: string
      [<FromForm>]
      comment: string
      [<FromForm>]
      seedKind: string
      eventHandler: EventHandler
      context: HttpContext
      plantRepository: PlantRepository }

let routes =
    let plantEventEndpoint (createEvent: Plant -> UserEventPayload) =
        fun (req: PlantEventParams) ->
            task {
                let plantId = PlantId req.plantId
                let! plant = req.plantRepository.get plantId

                let handlePlant plant =
                    createEvent plant
                    |> req.eventHandler.handle
                    |> Task.map (
                        Result.mapError (fun e -> $"Could not handle order: {e}")
                        >> Result.createdOrBadRequest
                    )

                return! plant |> Option.map handlePlant |> Option.or404NotFoundTask
            }


    endpoints {
        requireAuthorization
        group "trigger"

        post "/wantPlant" (plantEventEndpoint AddedWant)
        post "/removeWant" (plantEventEndpoint RemovedWant)
        post "/removeSeeds" (plantEventEndpoint RemovedSeeds)

        endpoints {
            group "/addSeeds"

            post "/" (fun (req: AddSeedsParams) ->
                task {
                    let plantId = PlantId req.plantId

                    let! plant =
                        req.plantRepository.get plantId
                        |> Task.map (fun p ->
                            match p with
                            | Some p -> Ok p
                            | None -> Error $"Plant Id '{plantId} Does not exist!")

                    let comment =
                        if String.IsNullOrEmpty req.comment then
                            None
                        else
                            Some req.comment

                    let seedKind =
                        match req.seedKind with
                        | null -> Error "Seedkind was not set!"
                        | v ->
                            match v.ToLower() with
                            | "seed" -> Ok Seed
                            | "seedling" -> Ok Seedling
                            | "cutting" -> Ok Cutting
                            | "whole_plant" -> Ok WholePlant
                            | "wholeplant" -> Ok WholePlant
                            | other -> Error $"Seedkind '{other}' is unsupported."

                    let handlePlant (plant, seedKind) =
                        task {
                            let event =
                                AddedSeeds
                                    { plant = plant
                                      comment = comment
                                      seedKind = seedKind }

                            let! result = req.eventHandler.handle event

                            return!
                                match result with
                                | Ok _ ->
                                    req.context.Response.Headers.Append("HX-Trigger", "closeModal")
                                    Task.FromResult(Results.Created())
                                    // TODO: errors should pop up in UI.
                                | Error e -> Results.BadRequest $"Could not handle order: {e}" |> Task.FromResult
                        }

                    return!
                        plant
                        |> Result.join seedKind
                        |> Result.map handlePlant
                        |> (Result.mapError (req.layout.render >> Task.FromResult))
                        |> Result.unpack
                })

            get
                "/modal/{plantId}"
                (fun
                    (req:
                        {| renderPage: Htmx.PageBuilder
                           plants: PlantRepository
                           antiForgery: IAntiforgery
                           httpContext: HttpContext
                           plantId: string |}) ->
                    task {
                        let antiForgeryToken = req.antiForgery.GetAndStoreTokens(req.httpContext)
                        let! plant = req.plants.get (PlantId req.plantId) |> Task.map Option.orFail

                        return Pages.Modal.AddSeeds.render plant antiForgeryToken
                    })

        }
    }
