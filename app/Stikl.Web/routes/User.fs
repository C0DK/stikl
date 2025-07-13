module webapp.routes.User

open System.Text
open System.Threading
open System.Threading.Tasks
open FSharp.Control
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Stikl.Web.Pages
open type TypedResults
open webapp
open domain
open webapp.services.EventBroker
open webapp.Components.Htmx

let routes =
    endpoints {
        group "user"

        get
            "/"
            (fun
                (req:
                    {| layout: Layout.Builder
                       // TODO: create card builder instead.
                       pageBuilder: PageBuilder
                       users: UserStore |}) ->
                task {
                    let! users = req.users.GetAll()

                    let content = Pages.User.List.render users req.pageBuilder

                    return req.layout.render content
                })


        // If buttons are pressed on your OWN page, it is not refreshed with new users.
        get
            "/{username}"
            (fun
                (req:
                    {| layout: Layout.Builder
                       users: UserStore
                       username: string |}) ->

                req.users.Get(Username req.username)
                |> Task.map (
                    (fun u ->
                        match u with
                        | Some user -> Components.sse.streamDiv $"/user/{user.username}/sse/"
                        | None ->
                            Pages.NotFound.render
                                "User not found!"
                                $"""
                                <p class="text-center text-lg md:text-xl">
                                  Vi kunne desværre ikke finde {Components.Common.themeGradiantSpan req.username}
                                </p>
                                {Components.Search.Form.render}
                                """)
                    >> req.layout.render
                ))

        get
            "/{username}/sse"
            (fun
                (req:
                    {| pageBuilder: PageBuilder
                       response: HttpResponse
                       ctx: HttpContext
                       eventBroker: EventBroker
                       life: IHostApplicationLifetime
                       logger: ILogger<User>
                       users: UserStore
                       username: string |}) ->

                let cancellationTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        req.life.ApplicationStopping,
                        req.ctx.RequestAborted
                    )

                let cancellationToken = cancellationTokenSource.Token

                // TODO can we do some username parsing/validation?
                // only done to test if we should 404
                req.users.Get(Username req.username)
                |> Task.collect (
                    Option.map (fun user ->
                        task {
                            let renderPage () =
                                task {
                                    // refresh user
                                    let! updatedUser = req.users.Get(Username req.username) |> Task.map Option.orFail
                                    return! Pages.User.Details.render updatedUser req.pageBuilder
                                }

                            let! initialPage = renderPage ()

                            do!
                                req.eventBroker.Listen cancellationToken
                                |> TaskSeq.filter (fun event -> event.user = user.username)
                                |> TaskSeq.mapAsync (fun _ -> renderPage ())
                                |> Components.sse.stream req.response initialPage
                        })
                    >> Option.defaultWith (fun () -> Components.sse.NotFound404 req.response cancellationToken)

                ))

    }
