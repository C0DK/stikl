module webapp.routes.Trigger

open System.Threading.Tasks
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Mvc
open type TypedResults
open webapp
open webapp.services
open webapp.Composition
open domain

type EventHandler =
    { handle: UserEvent -> Username -> Result<UserEvent, string> Task }

type PlantEventParams =
    { pageBuilder: Htmx.PageBuilder
      principal: Principal
      [<FromForm>]
      plantId: string
      users: User.UserSource
      eventHandler: EventHandler
      plantRepository: PlantRepository }

let routes =
    let plantEventEndpoint (createEvent: Plant -> UserEvent) =
        fun (req: PlantEventParams) ->
            task {
                let plantId = PlantId req.plantId

                let! user = req.users.getFromPrincipal () |> Task.map Option.orFail

                let! plant = req.plantRepository.get plantId

                return!
                    match plant with
                    | Some plant ->
                        task {
                            let event = createEvent plant

                            let! result = req.eventHandler.handle event user.username
                            // TODO: handle result? or not have it?

                            // TODO does this actually check the new state? this requires NO eventual consistency.
                            return! req.pageBuilder.plantCard plant |> Task.map Result.Html.Ok
                        }
                    | None -> Task.FromResult(req.pageBuilder.toPage $"404! - could not find {plantId}")

            }


    endpoints {
        requireAuthorization
        group "trigger"

        post "/wantPlant" (plantEventEndpoint AddedWant)
        post "/removeWant" (plantEventEndpoint RemovedWant)
    }
