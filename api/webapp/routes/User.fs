module webapp.routes.User

open System.Text
open System.Threading
open System.Threading.Tasks
open FSharp.Control
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
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
                    {| pageBuilder: PageBuilder
                       users: UserStore |}) ->
                task {
                    let! users = req.users.GetAll()

                    let! content = Pages.User.List.render users req.pageBuilder

                    return! req.pageBuilder.toPage content
                })


        // If buttons are pressed on your OWN page, it is not refreshed with new users.
        get
            "/{username}"
            (fun
                (req:
                    {| pageBuilder: PageBuilder
                       users: UserStore
                       username: string |}) ->

                req.users.Get(Username req.username)
                |> Task.collect (
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
                    >> req.pageBuilder.toPage
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
                req.users.Get(Username req.username)
                |> Task.map (
                    Option.map (fun user ->
                        task {
                            let renderPage () =
                                Pages.User.Details.render user req.pageBuilder

                            let! initialPage = renderPage ()

                            try
                                do!
                                    req.eventBroker.Listen cancellationToken
                                    |> TaskSeq.filter (fun event -> event.user = user.username)
                                    |> TaskSeq.mapAsync (fun _ -> renderPage ())
                                    |> Components.sse.stream req.response initialPage
                            with :? TaskCanceledException ->
                                printf "meh"
                        })
                    >> Option.defaultWith (fun () -> Components.sse.NotFound404 req.response cancellationToken)
                ))

    }
