module Stikl.Web.routes.Chat

open System.Threading
open FSharp.Control
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Mvc
open Stikl.Web.Components
open Stikl.Web.Pages
open Stikl.Web
open domain

let userNotFound (username: string) =
    // TODO: localize
    Results.NotFound($"User '{username}' could not be found")

type SendMessageParams =
    { username: string
      [<FromForm>]
      message: string
      users: UserStore
      layout: Layout.Builder
      context: HttpContext
      identity: CurrentUser
      locale: Localization
      eventHandler: UserEventHandler
      antiForgery: IAntiforgery
      cancellationToken: CancellationToken }

let getUser (username: string) (users: UserStore) (cancellationToken: CancellationToken) =
    users.Get (Username username) cancellationToken
    |> Task.map (fun user ->
        match user with
        | Some user -> Ok user
        | None -> Error(userNotFound username))

let routes =
    endpoints {
        group "chat"

        requireAuthorization

        endpoints {
            group "{username}"

            get
                ""
                (fun
                    (req:
                        {| layout: Layout.Builder
                           users: UserStore
                           context: HttpContext
                           identity: CurrentUser
                           locale: Localization
                           eventBroker: EventBroker
                           antiForgery: IAntiforgery
                           cancellationToken: CancellationToken
                           username: string |}) ->
                    let identity = req.identity.get |> Option.orFail

                    getUser req.username req.users req.cancellationToken
                    |> Task.map (
                        Result.map (fun user ->
                            if HttpRequest.IsSSE req.context.Request then
                                req.eventBroker.Listen(req.cancellationToken)
                                |> TaskSeq.filter (fun e ->
                                    e.user = identity.username
                                    && match e.payload with
                                       | UpdateName(_, __) -> true
                                       | MessageSent(_, receiver) -> receiver = user.username
                                       | MessageReceived(_, sender) -> sender = user.username
                                       | _ -> false)
                                |> TaskSeq.mapAsync (fun _ ->
                                    req.users.Get identity.username req.cancellationToken
                                    |> Task.map (
                                        Option.orFail
                                        >> _.chats
                                        >> Map.tryFind user.username
                                        >> Option.defaultValue []
                                        >> Pages.Chat.Chat.chatHistory
                                    ))
                                |> Sse.iresult
                            else
                                let antiForgeryToken = req.antiForgery.GetAndStoreTokens(req.context)
                                let chat = identity.chats |> Map.tryFind user.username |> Option.defaultValue []

                                Pages.Chat.Chat.render user chat antiForgeryToken req.locale
                                |> req.layout.render)
                        >> Result.unpack
                    ))

            post "" (fun (req: SendMessageParams) ->

                let identity = req.identity.get |> Option.orFail

                getUser req.username req.users req.cancellationToken
                |> Task.collect (fun r ->
                    // TODO: fail on empty message?
                    match r with
                    | Ok user ->
                        let events =
                            [ (UserEvent.create (MessageSent(req.message, user.username)) identity.username)
                              (UserEvent.create (MessageReceived(req.message, identity.username)) user.username) ]

                        req.eventHandler.handleMultiple events req.cancellationToken
                        |> Task.map (
                            (Result.map (fun _ ->

                                let antiForgeryToken = req.antiForgery.GetAndStoreTokens(req.context)

                                Result.Html.Ok(
                                    Pages.Chat.Chat.chatInputField user.username antiForgeryToken req.locale
                                )))
                            >> Toast.errorToResult
                        )
                    | Error e -> Task.fromResult e))
        }

    }
