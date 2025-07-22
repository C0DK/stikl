module Stikl.Web.routes.Trigger

open System

open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Mvc
open Stikl.Web.Pages
open Stikl.Web
open Stikl.Web.Components
open domain


type PlantEventParams =
    { [<FromForm>]
      plantId: string
      eventHandler: EventHandler
      cancellationToken: CancellationToken
      context: HttpContext
      plantRepository: PlantRepository }

type AddSeedsParams =
    { layout: Layout.Builder
      [<FromForm>]
      plantId: string
      [<FromForm>]
      comment: string
      [<FromForm>]
      seedKind: string
      eventHandler: EventHandler
      context: HttpContext
      cancellationToken: CancellationToken
      plantRepository: PlantRepository }

let routes =
    let plantEventEndpoint (createEvent: Plant -> UserEventPayload) =
        fun (req: PlantEventParams) ->
            task {
                let plantId = PlantId req.plantId
                let! plant = req.plantRepository.get plantId

                let handlePlant plant =
                    
                    req.eventHandler.handle (createEvent plant) req.cancellationToken
                    |> Task.map (
                        Result.mapError (fun e -> $"Could not handle order: {e}")
                        >> Result.map (fun _ -> Results.Created())
                        >> Message.errorToResult
                    )

                // return plant card instead, and update same card if it is placed multiple places on page.
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
                        let event = 
                            AddedSeeds
                                { plant = plant
                                  comment = comment
                                  seedKind = seedKind }
                        req.eventHandler.handle event req.cancellationToken 
                        |> Task.map (
                            Result.map (fun _ ->
                                req.context.Response.Headers.Append("HX-Trigger", "closeModal")
                                Results.Created())
                            >> Message.errorToResult
                        )

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
                        {| plants: PlantRepository
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
