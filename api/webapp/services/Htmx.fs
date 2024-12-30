module webapp.services.Htmx

open System.Threading.Tasks
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc.ViewFeatures
open Microsoft.Extensions.DependencyInjection
open domain
open webapp.services.User

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
    |> Result.Html.Ok


let plantCard
    (viewer:
        {| liked: bool
           antiForgeryToken: AntiforgeryTokenSet |} option)
    (plant: Plant)
    =
    let actions =
        match viewer with
        | Some viewer ->
            $"""
            <form
                hx-trigger="submit"
                hx-post="/trigger/{if viewer.liked then "removeWant" else "wantPlant"}"
                hx-target="closest #plant"
            >
                <input name="{viewer.antiForgeryToken.FormFieldName}" type="hidden" value="{viewer.antiForgeryToken.RequestToken}" />
                <input name="plantId" type="hidden" value="{plant.id}" />
                          
                <button
                    class="transform rounded-lg border-2 border-lime-600 px-3 py-1 font-sans text-xs font-bold text-lime-600 transition hover:scale-105"
                    type="submit">{if viewer.liked then "Dislike" else "Like"}</button>
            </form>"""
        | None -> ""

    $"""
<div
    id="plant"
    class="h-80 w-64 max-w-sm rounded-lg border border-gray-200 bg-white shadow"
>
    <img
        alt="Image of {plant.name}"
        class="h-3/4 w-64 rounded-t-lg border-b-2 border-gray-800 object-cover"
        src={plant.image_url}
    />
    <div class="float-right mr-2 mt-2 flex flex-col space-y-4">
        <a
            class="cursor-pointer text-sm text-lime-600 underline hover:text-lime-400"
            href="/plant/{plant.id}">Læs mere</a
        >
        {actions}
    </div>
    <h4 class="text-l mb-2 h-auto p-2 italic text-gray-600">
        {plant.name}
    </h4>
</div>
"""

type PageBuilder =
    { toPage: string -> IResult
      plantCard: Plant -> string Task }

let register (s: IServiceCollection) =
    s.AddScoped<PageBuilder>(fun s ->
        let principal = s.GetRequiredService<Option<Principal>>()
        let users = s.GetRequiredService<UserSource>()
        let antiForgery = s.GetRequiredService<IAntiforgery>()
        let httpContextAccessor = s.GetRequiredService<IHttpContextAccessor>()


        { toPage = fun content -> (renderPage content principal)
          plantCard =
            fun plant ->
                task {

                    let! user = users.getFromPrincipal ()

                    return
                        plantCard
                            (user
                             |> Option.map (fun user ->
                                 {| liked = User.Wants plant.id user
                                    antiForgeryToken = antiForgery.GetAndStoreTokens httpContextAccessor.HttpContext |}))
                            plant
                } })
