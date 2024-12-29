module webapp.Page

open Microsoft.AspNetCore.Http
open System.Text

let toOkResult (html: string) =
    Results.Text(html, "text/html", Encoding.UTF8, 200)

let header (user: Principal Option) =
    let profileButton =
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

let renderPage content (user: Principal Option) =
    $"""
	<!doctype html>
    <html lang="en">
      <head>
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <script src="https://unpkg.com/htmx.org@2.0.4" integrity="sha384-HGfztofotfshcF7+8n44JQL2oJmowVChPTg48S+jvZoztPfvwD79OC/LTtG6dMp+" crossorigin="anonymous"></script>
        <title>Stikl.dk</title>
        <script src="https://cdn.tailwindcss.com"></script>
      </head>
      <body>
        <div class="container mx-auto flex min-h-screen flex-col">
		  {header user}
          <main class="container mx-auto mt-10 flex flex-grow flex-col items-center space-y-8 p-2">
            {content}
          </main>
          <footer class="bg-lime flex w-full items-center justify-between p-4 text-slate-400">
            <p class="text-sm">© 2024 Stikling.io. All rights reserved.</p>
          </footer>
        </div>
      </body>
    </html>
"""
    |> toOkResult

type PageBuilder = { toPage: string -> IResult }
