module Stikl.Web.routes.Search

open System.Threading
open System.Threading.Tasks
open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Stikl.Web.Components
open Stikl.Web.services.User
open domain
open Stikl.Web.Composition
open FSharp.Control
open Stikl.Web
open Stikl.Web.services.EventBroker

let renderPage
    (query: string)
    (users: UserStore)
    (plantCardBuilder: Plant -> string)
    (cancellationToken: CancellationToken)
    =
    task {
        let query = query.ToLower()

        let plants = plants |> List.filter (_.name.ToLower().Contains(query))

        let! users = users.Query query cancellationToken

        return Search.Results.render plants users plantCardBuilder
    }

let routes =
    endpoints {
        group "search"

        get
            "/"
            (fun
                (req:
                    {| query: string
                       plantCardBuilder: PlantCard.Builder
                       users: UserStore
                       cancellationToken: CancellationToken |}) ->
                (renderPage req.query req.users req.plantCardBuilder.render req.cancellationToken)
                |> Task.map (fun page -> sse.streamDivWithInitialValue page $"search/sse?query={req.query}"))

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
                       identity: CurrentUser
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

                    let username = req.identity.get |> Option.map _.username

                    let fetchIdentity cancellationToken =
                        username
                        |> Option.map (fun u -> req.users.Get u cancellationToken)
                        |> Task.unpackOption

                    let eventStream = req.eventBroker.Listen cancellationToken

                    let eventStream =
                        match username with
                        | Some username -> eventStream |> TaskSeq.filter (fun event -> event.user = username)
                        | None -> eventStream

                    try
                        do!
                            eventStream
                            |> TaskSeq.mapAsync (fun _ ->
                                fetchIdentity cancellationToken
                                |> Task.collect (fun identity ->
                                    renderPage
                                        req.query
                                        req.users
                                        (req.plantCardBuilder.renderForIdentity identity)
                                        cancellationToken))
                            |> sse.stream req.response
                    with :? TaskCanceledException ->
                        printf "meh"
                })
    }
