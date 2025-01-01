module webapp.services.Htmx

open System.Threading.Tasks
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
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
        <script src="https://kit.fontawesome.com/ab39de689b.js" crossorigin="anonymous"></script>
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
           has: bool
           antiForgeryToken: AntiforgeryTokenSet |} option)
    (plant: Plant)
    =
    let actions =
        match viewer with
        | Some viewer ->
            let icon =
                if viewer.liked then
                    "<i class=\"fa-solid fa-heart\"></i>"
                else
                    "<i class=\"fa-regular fa-heart\"></i>"

            let actionButton
                (arg:
                    {| icon: string
                       postUrl: string
                       hxVals: string |})
                =
                $"""
                    <a
                        hx-post="{arg.postUrl}"
                        hx-target="closest #plant"
                        hx-vals='{arg.hxVals}'
                        class="text-lime-600 transition hover:text-lime-400"
                        type="submit">
                        <i class="fa-{arg.icon}"></i>
                    </a>
                """

            let addButton =
                actionButton (
                    if viewer.has then
                        {| icon = "solid fa-seedling"
                           hxVals =
                            $"{{\"plantId\":\"{plant.id}\", \"{viewer.antiForgeryToken.FormFieldName}\":\"{viewer.antiForgeryToken.RequestToken}\"}}"
                           // TODO: correct / better url
                           postUrl = "/trigger/removeSeeds" |}
                    else
                        {| icon = "solid fa-plus"
                           // TODO optional?
                           hxVals = ""
                           // TODO: get, not post?
                           postUrl = $"/trigger/addSeeds/modal/{plant.id}" |}
                )

            let likeButton =
                actionButton
                    {| icon =
                        if viewer.liked then
                            "solid fa-heart"
                        else
                            "regular fa-heart"
                       hxVals =
                        $"{{\"plantId\":\"{plant.id}\", \"{viewer.antiForgeryToken.FormFieldName}\":\"{viewer.antiForgeryToken.RequestToken}\"}}"
                       postUrl =
                        if viewer.liked then
                            "/trigger/removeWant"
                        else
                            "/trigger/wantPlant" |}

            $"""
            <div class="flex gap-2 justify-end">
                {likeButton}
                {addButton}
            </div>
            """
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
                                    has = User.Has plant.id user
                                    antiForgeryToken = antiForgery.GetAndStoreTokens httpContextAccessor.HttpContext |}))
                            plant
                } })
