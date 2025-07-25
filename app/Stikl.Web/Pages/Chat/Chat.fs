module Stikl.Web.Pages.Chat.Chat

open System
open Microsoft.AspNetCore.Antiforgery
open Stikl.Web
open Stikl.Web.Components
open domain

let heading (user: User) =
    //language=HTML
    $"""
     <div class="flex justify-self-start mb-5">
         <a href="/u/{user.username}">
            <img
                alt="Image of a {user.fullName |> String.escape}"
                class="p-2 aspect-square h-16 rounded-full object-cover"
                src="{user.imgUrl}"
            />
        </a>
        <span class="inline content-center">
            <h1 class="font-sans text-3xl font-bold text-lime-800"><a href="/u/{user.username}">{user.fullName |> String.escape}</a></h1>
            <p class="pl-2 text-sm font-bold text-slate-600">
                {user.location.location.label}
            </p>
        </pan>
     </div>
     """

let chatMessage (message: Chat.Message) =
    let bgColor =
        match message.kind with
        | Chat.MessageReceived -> "bg-amber-50"
        | Chat.MessageSent -> "bg-green-50"

    let borderColor =
        match message.kind with
        | Chat.MessageReceived -> "border-amber-600"
        | Chat.MessageSent -> "border-green-600"

    let side =
        match message.kind with
        | Chat.MessageSent -> "ml-auto"
        | Chat.MessageReceived -> "mr-auto"
    // TODO: max height so it scrolls better.
    $"""
    <div class="{Theme.rounding} shadow-lg border-2 p-2 {bgColor} {borderColor} w-2/3 {side} text-wrap">
        <p>
            {message.content |> String.escape}
        </p>
        <p class="{Theme.textMutedColor} text-xs">{DateTimeOffset.formatRelative message.timestamp}</p>
    </div>
    """

let chatHistory (chat: Chat.Message list) =
    // TODO scroll bottom
    // TODO: make height better
    $"""
    <div
        class="w-full {Theme.rounding} max-w-3xl max-h-[60vh] border overflow-y-auto bg-white/50 p-3 inset-shadow-sm/20 flex flex-col-reverse gap-5"
    >
        {chat |> Seq.map chatMessage |> String.concat "\n"}
    </div>
    """

let chatInputField (recipient: Username) (antiForgeryToken: AntiforgeryTokenSet) (locale: Localization) =
    $$"""
    <input
        name="message"
        class="shadow appearance-none bg-white border {{Theme.rounding}} w-full max-w-2xl py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline"
        placeholder="{{locale.chat.writeAMessage}}"
        hx-headers='{"{{antiForgeryToken.HeaderName}}": "{{antiForgeryToken.RequestToken}}"}'
        hx-target="this"
        hx-swap="outerHTML"
        hx-post="/chat/{{recipient.value}}"
        hx-trigger="keyup[key=='Enter'] changed"
        autofocus
    />
    """

let render (user: User) (chat: Chat.Message list) (antiForgeryToken: AntiforgeryTokenSet) (locale: Localization) =
    "<div class=\"grid justify-items-center gap-4\">"
    + (heading user)
    + (Sse.streamDivWithInitialValue (chatHistory chat) $"/chat/{user.username}")
    + (chatInputField user.username antiForgeryToken locale)
    + "</div>"
