module webapp.routes.Trigger

open System.Threading.Tasks
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Mvc
open type TypedResults
open webapp
open webapp.Auth0
open webapp.Page
open domain

type PlantSource =
    { exists: PlantId -> bool Task
      get: PlantId -> Plant }

type EventHandler =
    {
      // TODO: return a result (of an event id maybe?)
      handle: Username -> UserEvent -> Task }

[<CLIMutable>]
type PlantPayload = { plantId: string }

type PlantEventParams =
    { pageBuilder: PageBuilder
      principal: Principal
      httpContext: HttpContext
      [<FromForm>]
      plantId: string
      userSource: UserSource
      eventHandler: EventHandler
      antiForgery: IAntiforgery
      plantSource: PlantSource }

let routes =
    let plantEventEndpoint (createEvent: PlantId -> UserEvent) =
        fun (req: PlantEventParams) ->
            task {
                let plantId = PlantId req.plantId

                let! user =
                    req.userSource.getUserById req.principal.auth0Id
                    |> Task.map (Option.defaultWith (fun () -> failwith "huh??"))

                let! exists = req.plantSource.exists plantId

                return!
                    match exists with
                    | true ->
                        task {
                            let event = createEvent plantId

                            do! req.eventHandler.handle (Username user.username) event

                            let plant = req.plantSource.get plantId

                            // TODO add the actual state (i.e. liked)
                            let token = req.antiForgery.GetAndStoreTokens(req.httpContext)

                            // TODO: get correct state - not just true.
                            return toOkResult (Components.authedPlantCard (Some(true, token)) plant)
                        }
                    | false -> Task.FromResult(req.pageBuilder.toPage $"404! - could not find {plantId}")

            }


    endpoints {
        requireAuthorization
        group "trigger"

        post "/wantPlant" (plantEventEndpoint AddedWant)
        post "/removeWant" (plantEventEndpoint RemovedWant)
    }
