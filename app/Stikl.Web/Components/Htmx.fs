module Stikl.Web.Components.Htmx

open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open domain
open Stikl.Web
open Stikl.Web.services.User

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

    Components.Common.imgCard
        cardId
        {| alt = $"Image of {plant.name}"
           src = plant.image_url |}
        plant.name
        ($"<a class='cursor-pointer text-sm text-lime-600 underline hover:text-lime-400' href='/plant/{plant.id}'>Læs mere</a>"
         + actions)


let userCard (user: domain.User) =
    let name = user.lastName |> Option.defaultValue user.username.value

    Components.Common.imgCard
        "user"
        {| alt = $"Image of {name}"
           src = user.imgUrl |}
        name
        $"<a class='cursor-pointer text-sm text-lime-600 underline hover:text-lime-400' href='/user/{user.username.value}'>Se profil</a>"

type PageBuilder =
    { plantCard: Plant -> string
      userCard: User -> string }

let register (s: IServiceCollection) =
    s.AddScoped<PageBuilder>(fun s ->
        let currentUser = s.GetRequiredService<CurrentUser>()
        let antiForgery = s.GetRequiredService<IAntiforgery>()
        let httpContextAccessor = s.GetRequiredService<IHttpContextAccessor>()

        { plantCard =
            fun plant ->
                plantCard
                    (currentUser.get
                     |> Option.map (fun user ->
                         {| liked = User.Wants plant.id user
                            has = User.Has plant.id user
                            antiForgeryToken = antiForgery.GetAndStoreTokens httpContextAccessor.HttpContext |}))
                    plant
          userCard = userCard })
