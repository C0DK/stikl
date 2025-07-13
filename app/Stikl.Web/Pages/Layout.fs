module Stikl.Web.Pages.Layout


open System
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open domain
open webapp.services.User

let header (user: User Option) =
    let profileButton =
        // TODO: check expired (possibly in the principal level)
        match user with
        | Some user ->
            $"""
            <a
                class="transform px-3 py-1 font-sans text-sm font-bold text-lime-600 underline transition"
                href="/auth/profile"
            >
             Hi, {user.firstName |> Option.defaultValue user.username.value}	
            </a>
"""
        | None ->
            """
            <a
                class="transform rounded-lg border-2 border-lime-600 px-3 py-1 font-sans text-sm font-bold text-lime-600 transition hover:scale-105"
                href="/auth/login"
            >
                Log ind
            </a>
"""

    $"""
    <header class="bg-lime-30 flex justify-between p-2">
        <a
            class="rounded-lg bg-gradient-to-br from-lime-600 to-amber-600 px-3 py-1 text-left font-sans text-xl font-semibold text-white hover:underline"
            href="/">Stikl.dk</a
        >
        <div class="flex justify-between gap-5">
            {profileButton}
        </div>
    </header>
    """
    
let modalId = "modals-here"

let render content (user: User Option) =
    // language=html
    $"""
	<!doctype html>
    <html lang="en">
      <head>
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <script src="https://cdn.jsdelivr.net/npm/htmx.org@2.0.6/dist/htmx.min.js" crossorigin="anonymous"></script>
        <script src="https://cdn.jsdelivr.net/npm/htmx-ext-sse@2.2.2"  crossorigin="anonymous"></script>
        <script src="https://unpkg.com/hyperscript.org@0.9.13"></script>
        <script src="https://kit.fontawesome.com/ab39de689b.js" crossorigin="anonymous"></script>
        <title>Stikl.dk</title>
        <script src="https://cdn.tailwindcss.com"></script>
      </head>
      <body hx-ext="sse" >
        <div id="{modalId}"></div>
		{header user}
        <div class="container mx-auto flex min-h-screen flex-col">
          <main class="container mx-auto mt-10 flex flex-grow flex-col items-center space-y-8 p-2">
            {content}
          </main>
          <footer class="bg-lime flex w-full items-center justify-between p-4 text-slate-400">
            <p class="text-sm">© {DateTimeOffset.UtcNow.Year} Stikling.io. All rights reserved.</p>
          </footer>
        </div>
      </body>
    </html>
"""
    |> Result.Html.Ok

type ActionRequest =
    | Post of url: string * hxVals: string
    | Get of url: string

// TODO: use same icon for "on" and "off", but change background or something
let actionButton
    (arg:
        {| icon: string
           request: ActionRequest
           hxTarget: string |})
    =
    let requestProperties =
        match arg.request with
        | Get url -> $"hx-get=\"{url}\""
        | Post(url, hxVals) -> $"hx-post=\"{url}\" hx-vals='{hxVals}'"

    $"""
    <a
        {requestProperties}
        hx-target="{arg.hxTarget}"
        class="text-lime-600 transition hover:text-lime-400"
        type="submit">
        <i class="fa-{arg.icon}"></i>
    </a>
    """


type Builder =
    { render: string -> IResult
      }
let register (s: IServiceCollection) =
    s.AddScoped<Builder>(fun s ->
        let currentUser = s.GetRequiredService<CurrentUser>()
        let antiForgery = s.GetRequiredService<IAntiforgery>()
        let httpContextAccessor = s.GetRequiredService<IHttpContextAccessor>()

        { render = fun content -> render content currentUser.get })
