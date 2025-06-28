module webapp.routes.User

open System.Text
open System.Threading
open FSharp.Control
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open type TypedResults
open webapp
open domain
open webapp.services
open webapp.services.EventBroker
open webapp.services.Htmx

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
                task {
                    // TODO: parse/verify username
                    let! userOption = req.users.Get(Username req.username)

                    return!
                        match userOption with
                        | Some user -> sse.renderPage $"/user/{user.username}/sse/" req.pageBuilder
                        | None ->
                            Pages.NotFound.render
                                "User not found!"
                                $"""
                                <p class="text-center text-lg md:text-xl">
                                  Vi kunne desværre ikke finde {Components.themeGradiantSpan req.username}
                                </p>
                                {Components.search}
                                """
                            |> Task.collect req.pageBuilder.toPage

                })

        get
            "/{username}/sse"
            (fun
                (req:
                    {| pageBuilder: PageBuilder
                       ctx: HttpContext
                       resp: HttpResponse
                       eventBroker: EventBroker
                       life: IHostApplicationLifetime
                       logger: ILogger<User>
                       users: UserStore
                       username: string |}) ->
                task {
                    // TODO: parse/verify username
                    let! userOption = req.users.Get(Username req.username)

                    let cancellationTokenSource =
                        CancellationTokenSource.CreateLinkedTokenSource(
                            req.life.ApplicationStopping,
                            req.ctx.RequestAborted
                        )

                    let response = req.resp
                    let cancellationToken = cancellationTokenSource.Token

                    return!
                        match userOption with
                        | Some user ->
                            task {
                                let! init = Pages.User.Details.render user req.pageBuilder
                                    
                                    // TODO: make it actually init!
                                req.logger.LogInformation "start?";
                                do!
                                    init
                                    |> TaskSeq.singleton
                                    |> TaskSeq.append(
                                        req.eventBroker.Listen cancellationToken
                                        |> TaskSeq.filter (fun event -> event.user = user.username)
                                        |> TaskSeq.mapAsync (fun _ -> Pages.User.Details.render user req.pageBuilder)
                                    )
                                    |> sse.stream response
                            }
                        | None ->
                            task {
                                // TODO: better
                                let payload = Encoding.UTF8.GetBytes("404\n\n")
                                response.StatusCode <- 404
                                do! response.Body.WriteAsync(payload, cancellationToken)
                            }
                })

    }
