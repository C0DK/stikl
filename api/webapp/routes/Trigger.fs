module webapp.routes.Trigger

open System.Threading.Tasks
open Microsoft.AspNetCore.Antiforgery
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

[<CLIMutable>]
type PlantPayload = { plantId: string }

type PlantEventParams =
    { pageBuilder: Page.PageBuilder
      principal: Principal
      httpContext: HttpContext
      [<FromForm>]
      plantId: string
      users: User.UserSource
      eventHandler: EventHandler
      antiForgery: IAntiforgery
      plantRepository: PlantRepository }

let routes =
    let plantEventEndpoint (createEvent: Plant -> UserEvent) =
        fun (req: PlantEventParams) ->
            task {
                let plantId = PlantId req.plantId

                let! user =
                    req.users.getUserById req.principal.auth0Id
                    |> Task.map (Option.defaultWith (fun () -> failwith "huh??"))

                let! plant = req.plantRepository.get plantId

                return!
                    match plant with
                    | Some plant ->
                        task {
                            let event = createEvent plant

                            let! result = req.eventHandler.handle event user.username
                            // TODO: handle result? or not have it?

                            // TODO add the actual state (i.e. liked)
                            let token = req.antiForgery.GetAndStoreTokens(req.httpContext)

                            let newUser = apply event user
                            // TODO move to pagebuilder so we get user state for free.
                            return
                                Result.Html.Ok(
                                    Components.authedPlantCard (Some(User.Wants plant.id newUser, token)) plant
                                )
                        }
                    | None -> Task.FromResult(req.pageBuilder.toPage $"404! - could not find {plantId}")

            }


    endpoints {
        requireAuthorization
        group "trigger"

        post "/wantPlant" (plantEventEndpoint AddedWant)
        post "/removeWant" (plantEventEndpoint RemovedWant)
    }
