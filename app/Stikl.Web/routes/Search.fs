module Stikl.Web.routes.Search

open System.Threading
open System.Threading.Tasks
open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Stikl.Web.Components
open domain
open Stikl.Web.Composition
open FSharp.Control
open Stikl.Web
open Stikl.Web.services.EventBroker

let routes =
    endpoints {
        group "search"

        get "/" (fun (req: {| query: string |}) -> sse.streamDiv $"search/sse?query={req.query}")

        get
            "/sse"
            (fun
                (req:
                    {| query: string
                       cancellationToken: CancellationToken
                       antiForgery: IAntiforgery
                       plants: PlantRepository
                       plantCardBuilder: PlantCard.Builder
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

                            return Search.Results.render plants users req.plantCardBuilder
                        }

                    let! initialPage = renderPage ()

                    try
                        do!
                            req.eventBroker.Listen cancellationToken
                            |> TaskSeq.mapAsync (fun _ -> renderPage ())
                            |> sse.stream req.response initialPage
                    with :? TaskCanceledException ->
                        printf "meh"
                })
    }
