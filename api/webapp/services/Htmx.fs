module webapp.services.Htmx

open System.Threading.Tasks
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open domain
open webapp
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

let renderPage content (user: User Option) =
    $"""
	<!doctype html>
    <html lang="en">
      <head>
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <script src="https://unpkg.com/htmx.org@2.0.4" integrity="sha384-HGfztofotfshcF7+8n44JQL2oJmowVChPTg48S+jvZoztPfvwD79OC/LTtG6dMp+" crossorigin="anonymous"></script>
        <script src="https://unpkg.com/hyperscript.org@0.9.13"></script>
        <script src="https://kit.fontawesome.com/ab39de689b.js" crossorigin="anonymous"></script>
        <title>Stikl.dk</title>
        <script src="https://cdn.tailwindcss.com"></script>
      </head>
      <body>
        <div id="modals-here"></div>
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

let modal title content =
    // TODO: handle click on overlay..
    $"""
<div
    id="modal"
    role="dialog"
    tabindex="-1"
    _="on closeModal remove me"
    class="fixed z-50 inset-0 bg-gray-900 bg-opacity-60 overflow-y-auto h-full w-full px-4"
    >
    <div class="relative top-40 mx-auto shadow-xl rounded-md bg-white max-w-md">
        <div class="relative bg-white rounded-lg shadow">
            <div class="flex justify-end p-2">
                <h1 class="font-sans text-xl">
                    {title}
                </h1>
                <button
                    _="on click trigger closeModal"
                    type="button"
                    class="text-gray-400 bg-transparent hover:bg-gray-200 hover:text-gray-900 rounded-lg text-sm p-1.5 ml-auto inline-flex items-center"
                    >
                    <i class="fa-solid fa-xmark"></i>
                </button>
            </div>
            {content}
	    </div>
	</div>
</div>
"""

let modalSelector = "#modals-here"

let plantCard
    (viewer:
        {| liked: bool
           has: bool
           antiForgeryToken: AntiforgeryTokenSet |} option)
    (plant: Plant)
    =
    let cardId = $"plant-{plant.id}"

    let actions =
        match viewer with
        | Some viewer ->

            let plantIdPayload =
                $"{{\"plantId\":\"{plant.id}\", \"{viewer.antiForgeryToken.FormFieldName}\":\"{viewer.antiForgeryToken.RequestToken}\"}}"

            let addButton =
                actionButton (
                    if viewer.has then
                        {| icon = "solid fa-seedling"
                           hxTarget = $"#{cardId}"
                           request = Post("/trigger/removeSeeds", plantIdPayload) |}
                    else
                        {| icon = "solid fa-plus"
                           hxTarget = modalSelector
                           request = Get $"/trigger/addSeeds/modal/{plant.id}" |}
                )

            let likeButton =
                actionButton (
                    if viewer.liked then
                        {| icon = "solid fa-heart"
                           hxTarget = $"#{cardId}"
                           request = Post("/trigger/removeWant", plantIdPayload) |}
                    else
                        {| icon = "regular fa-heart"
                           hxTarget = $"#{cardId}"
                           request = Post("/trigger/wantPlant", plantIdPayload) |}
                )

            $"""
            <div class="flex gap-2 justify-end">
                {likeButton}
                {addButton}
            </div>
            """
        | None -> ""

    Components.imgCard
        cardId
        {| alt = $"Image of {plant.name}"
           src = plant.image_url |}
        plant.name
        ($"<a class='cursor-pointer text-sm text-lime-600 underline hover:text-lime-400' href='/plant/{plant.id}'>Læs mere</a>"
         + actions)


let userCard (user: domain.User) =
    let name = user.fullName |> Option.defaultValue user.username.value

    Components.imgCard
        "user"
        {| alt = $"Image of {name}"
           src = user.imgUrl |}
        name
        $"<a class='cursor-pointer text-sm text-lime-600 underline hover:text-lime-400' href='/user/{user.username.value}'>Se profil</a>"
    |> Task.FromResult

type PageBuilder =
    { toPage: string -> IResult Task
      plantCard: Plant -> string Task
      userCard: User -> string Task }

let register (s: IServiceCollection) =
    s.AddScoped<PageBuilder>(fun s ->
        let currentUser = s.GetRequiredService<CurrentUser>()
        let antiForgery = s.GetRequiredService<IAntiforgery>()
        let httpContextAccessor = s.GetRequiredService<IHttpContextAccessor>()

        { toPage = fun content -> currentUser.get () |> Task.map (renderPage content)
          plantCard =
            fun plant ->
                task {

                    let! user = currentUser.get ()

                    return
                        plantCard
                            (user
                             |> Option.map (fun user ->
                                 {| liked = User.Wants plant.id user
                                    has = User.Has plant.id user
                                    antiForgeryToken = antiForgery.GetAndStoreTokens httpContextAccessor.HttpContext |}))
                            plant
                }
          userCard = userCard })
