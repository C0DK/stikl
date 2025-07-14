module webapp.routes.Search

open System.Threading
open System.Threading.Tasks
open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open domain
open webapp.Components.Htmx
open webapp.Composition
open webapp.services
open FSharp.Control
open webapp
open webapp.services.EventBroker

let routes =
    endpoints {
        group "search"

        get "/" (fun (req: {| query: string |}) -> Components.sse.streamDiv $"search/sse?query={req.query}")

        get
            "/sse"
            (fun
                (req:
                    {| query: string
                       cancellationToken: CancellationToken
                       antiForgery: IAntiforgery
                       plants: PlantRepository
                       pageBuilder: PageBuilder
                       response: HttpResponse
                       ctx: HttpContext
                       eventBroker: EventBroker
                       life: IHostApplicationLifetime
                       users: UserStore |}) ->
                task {
                    let cancellationTokenSource =
                        CancellationTokenSource.CreateLinkedTokenSource(
                            req.life.ApplicationStopping,
                            req.ctx.RequestAborted
                        )

                    let cancellationToken = cancellationTokenSource.Token

                    let renderPage () =
                        task {
                            let query = req.query.ToLower()

                            let plants = plants |> List.filter (_.name.ToLower().Contains(query))

                            let! users = req.users.Query query

                            // TODO: the pagebuilder doesnt refresh the currentUser
                            return Components.Search.Results.render plants users req.pageBuilder
                        }

                    let! initialPage = renderPage ()

                    try
                        do!
                            req.eventBroker.Listen cancellationToken
                            |> TaskSeq.mapAsync (fun _ -> renderPage ())
                            |> Components.sse.stream req.response initialPage
                    with :? TaskCanceledException ->
                        printf "meh"
                })
    }
