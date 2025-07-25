module Stikl.Web.routes.User

open System.Threading
open FSharp.Control
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Stikl.Web.Components
open Stikl.Web.Pages
open Stikl.Web.services.User
open type TypedResults
open Stikl.Web
open domain
open Stikl.Web.services.EventBroker

type SendMessageParams =
    { username: string
      [<FromForm>]
      message: string
      users: UserStore
      layout: Layout.Builder
      context: HttpContext
      identity: CurrentUser
      locale: Localization
      eventHandler: EventHandler
      antiForgery: IAntiforgery
      cancellationToken: CancellationToken }

let userNotFound username =
    Pages.NotFound.render
        "User not found!"
        $"""
        <p class="text-center text-lg md:text-xl">
          Vi kunne desværre ikke finde {ThemeGradiantSpan.render username}
        </p>
        {Search.Form.render}
        """

let routes =
    endpoints {
        group "u"

        get
            "/"
            (fun
                (req:
                    {| layout: Layout.Builder
                       cancellationToken: CancellationToken
                       users: UserStore |}) ->
                req.users.GetAll req.cancellationToken
                |> Task.map (Pages.User.List.render >> req.layout.render))


        // If buttons are pressed on your OWN page, it is not refreshed with new users.
        get
            "/{username}"
            (fun
                (req:
                    {| layout: Layout.Builder
                       users: UserStore
                       plantCardBuilder: PlantCard.Builder
                       cancellationToken: CancellationToken
                       username: string |}) ->

                req.users.Get (Username req.username) req.cancellationToken
                |> Task.map (
                    (fun u ->
                        match u with
                        | Some user -> Pages.User.Details.renderWithStream user req.plantCardBuilder
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
                       identity: CurrentUser
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
                let username = (Username req.username)

                req.users.Get username cancellationToken
                |> Task.collect (
                    Option.map (fun user ->
                        task {
                            let renderPage (event: UserEvent) =
                                task {
                                    // refresh user. could also just apply event, if we keep prior, like a fold-ish thing?
                                    let! updatedUser =
                                        req.users.Get username cancellationToken |> Task.map Option.orFail
                                    // only if event on current user?
                                    let! updatedIdentity =
                                        req.identity.get
                                        |> Option.map (fun u -> req.users.Get u.username cancellationToken)
                                        |> Task.unpackOption

                                    return
                                        Pages.User.Details.render
                                            updatedUser
                                            (req.plantCardBuilder.renderForIdentity updatedIdentity)
                                }

                            let eventStream =
                                req.eventBroker.Listen cancellationToken
                                |> TaskSeq.filter (fun event -> event.user = user.username)

                            let eventStream =
                                match req.identity with
                                | AuthedUser requestingUser ->
                                    eventStream
                                    |> TaskSeq.filter (fun event -> event.user = requestingUser.username)
                                | _ -> eventStream

                            do! eventStream |> TaskSeq.mapAsync renderPage |> sse.stream req.response
                        })
                    >> Option.defaultWith (fun () -> sse.NotFound404 req.response cancellationToken)

                ))

        endpoints {
            group "/{username}/chat"

            requireAuthorization

            get
                ""
                (fun
                    (req:
                        {| layout: Layout.Builder
                           users: UserStore
                           context: HttpContext
                           identity: CurrentUser
                           locale: Localization
                           antiForgery: IAntiforgery
                           cancellationToken: CancellationToken
                           username: string |}) ->
                    if HttpRequest.IsSSE req.context.Request then
                        failwith "TODO"

                    let identity = req.identity.get |> Option.orFail

                    req.users.Get (Username req.username) req.cancellationToken
                    |> Task.map (
                        fun u ->
                            match u with
                            | Some user ->
                                let antiForgeryToken = req.antiForgery.GetAndStoreTokens(req.context)
                                let chat = identity.chats |> Map.tryFind user.username |> Option.defaultValue []
                                Pages.User.Chat.render user chat antiForgeryToken req.locale
                            | None -> userNotFound req.username
                        >> req.layout.render
                    ))

            post "" (fun (req: SendMessageParams) ->

                let identity = req.identity.get |> Option.orFail

                req.users.Get (Username req.username) req.cancellationToken
                |> Task.collect (fun u ->
                    match u with
                    | Some user ->
                        let events =
                            [ (UserEvent.create (MessageSent(req.message, user.username)) identity.username)
                              (UserEvent.create (MessageReceived(req.message, identity.username)) user.username) ]

                        req.eventHandler.handleMultiple events req.cancellationToken
                        |> Task.map (
                            // TODO use partial instead.
                            (Result.map (fun _ ->
                                
                                let antiForgeryToken = req.antiForgery.GetAndStoreTokens(req.context)
                                Result.Html.Ok(Pages.User.Chat.chatInputField user.username antiForgeryToken req.locale)))
                            >> Toast.errorToResult
                        )
                    | None -> Task.fromResult (userNotFound req.username |> req.layout.render)))
        }

    }
