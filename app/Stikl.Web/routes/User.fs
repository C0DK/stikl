module Stikl.Web.routes.User

open System.Threading
open FSharp.Control
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Stikl.Web.Components
open Stikl.Web.Pages
open type TypedResults
open Stikl.Web
open domain
open Stikl.Web.services.EventBroker

let routes =
    endpoints {
        group "user"

        get
            "/"
            (fun
                (req:
                    {| layout: Layout.Builder
                       // TODO: create card builder instead.
                       users: UserStore |}) ->
                     req.users.GetAll()
                     |> Task.map(Pages.User.List.render >> req.layout.render)
                )


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
                        | Some user -> sse.streamDiv $"/user/{user.username}/sse/"
                        | None ->
                            Pages.NotFound.render
                                "User not found!"
                                $"""
                                <p class="text-center text-lg md:text-xl">
                                  Vi kunne desværre ikke finde {ThemeGradiantSpan.render req.username}
                                </p>
                                {Search.Form.render}
                                """)
                    >> req.layout.render
                ))

        get
            "/{username}/sse"
            (fun
                (req:
                    {| plantCardBuilder: PlantCard.Builder
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
                                    return Pages.User.Details.render updatedUser req.plantCardBuilder
                                }

                            let! initialPage = renderPage ()

                            do!
                                req.eventBroker.Listen cancellationToken
                                |> TaskSeq.filter (fun event -> event.user = user.username)
                                |> TaskSeq.mapAsync (fun _ -> renderPage ())
                                |> sse.stream req.response initialPage
                        })
                    >> Option.defaultWith (fun () -> sse.NotFound404 req.response cancellationToken)

                ))

    }
