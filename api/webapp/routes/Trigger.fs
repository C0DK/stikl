module webapp.routes.Trigger

open System

open System.Threading.Tasks
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Mvc
open webapp
open webapp.Components
open webapp.services
open webapp.Composition
open domain

type EventHandler =
    { handle: UserEvent -> Result<UserEvent, string> Task }


type PlantEventParams =
    { pageBuilder: Htmx.PageBuilder
      [<FromForm>]
      plantId: string
      eventHandler: EventHandler
      context: HttpContext
      plantRepository: PlantRepository }

type AddSeedsParams =
    { pageBuilder: Htmx.PageBuilder
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
    let plantEventEndpoint (createEvent: Plant -> UserEvent) =
        fun (req: PlantEventParams) ->
            task {
                let plantId = PlantId req.plantId
                let! plant = req.plantRepository.get plantId

                let handlePlant plant =
                    task {
                        let event = createEvent plant

                        let! result = req.eventHandler.handle event
                        // TODO: does this actually check the new state? this requires NO eventual consistency.
                        return!
                            match result with
                            | Ok _ -> req.pageBuilder.plantCard plant |> Task.map Result.Html.Ok
                            | Error e -> Results.BadRequest $"Could not handle order: {e}" |> Task.FromResult
                    }

                return!
                    plant
                    |> Option.map handlePlant
                    |> Task.unpackOptionTask
                    |> Task.map (Option.defaultValue (req.pageBuilder.toPage $"404! - could not find {plantId}"))
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
                                | Ok v ->
                                    req.context.Response.Headers.Append("HX-Trigger", "closeModal")
                                    req.pageBuilder.plantCard plant |> Task.map Result.Html.Ok
                                | Error e -> Results.BadRequest $"Could not handle order: {e}" |> Task.FromResult
                        }

                    return!
                        plant
                        |> Result.join seedKind
                        |> Result.map handlePlant
                        |> (Result.mapError (req.pageBuilder.toPage >> Task.FromResult))
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
                        // TODO Better 404?

                        let antiForgeryToken = req.antiForgery.GetAndStoreTokens(req.httpContext)
                        let! plant = req.plants.get (PlantId req.plantId) |> Task.map Option.orFail

                        return
                            Result.Html.Ok(
                                Htmx.modal
                                    $"Tilføj{themeGradiantSpan plant.name}frø"
                                    $"""
                            <form
                                hx-post="/trigger/addSeeds/"
                                hx-target="#plant-{plant.id}"
                                _="on htmx:afterRequest trigger closeModal"
                                class="p-4"
                                >
                                <div class="mb-4">
                                    <label class="block text-gray-700 text-sm font-bold mb-2" for="username">
                                        Kommentar
                                    </label>
                                    <input type="hidden" name="{antiForgeryToken.FormFieldName}" value="{antiForgeryToken.RequestToken}"/>
                                    <input type="hidden" name="plantId" value="{req.plantId}"/>
                                    <input
                                        class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline"
                                        id="comment"
                                        name="comment"
                                        type="text" placeholder="Kommentarer til potentielle interesserede">
                                </div>
                                <div class="mb-4">
                                    <label class="block text-gray-700 text-sm font-bold mb-2" for="seedKind">
                                        Type
                                    </label>
                                    <div class="relative">
                                        <select
                                            id="seedKind"
                                            name="seedKind"
                                            class="block appearance-none w-full bg-gray-200 border border-gray-200 text-gray-700 py-3 px-4 pr-8 rounded leading-tight focus:outline-none focus:bg-white focus:border-gray-500"
                                            >
                                            <option value="Seed">Frø</option>
                                            <option value="Seedling">frøplante</option>
                                            <option value="Cutting">Stikling</option>
                                            <option value="WholePlant">Komplet plante</option>
                                        </select>
                                        <div class="pointer-events-none absolute inset-y-0 right-0 flex items-center px-2 text-gray-700">
                                            <i class="fa-solid fa-chevron-down"></i>
                                        </div>
                                    </div>
                                </div>
                                <button 
                                    type="submit" 
                                    class="transform rounded-lg border-2 border-lime-600 px-3 py-1 font-sans text-xs font-bold text-lime-600 transition hover:scale-105" 
                                    >Gem</button>
                            </form>
                            """
                            )
                    })

        }
    }
