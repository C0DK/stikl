module Stikl.Web.Components.PlantCard

open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Stikl.Web
open Stikl.Web.services.User
open domain

type ActionRequest =
    | Post of url: string * hxVals: string
    | Get of url: string

let href (plant: Plant) = $"/plant/{plant.id}"

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

let render
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
                           hxTarget = "#modals-here"
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

    Card.render
        cardId
        {| alt = $"Image of {plant.name}"
           src = plant.image_url |}
        plant.name
        actions
        (href plant)

type Builder = { render: Plant -> string }

let register (s: IServiceCollection) =
    s.AddScoped<Builder>(fun s ->
        let currentUser = s.GetRequiredService<CurrentUser>()
        let locale = Localization.``default``
        let antiForgery = s.GetRequiredService<IAntiforgery>()
        let httpContextAccessor = s.GetRequiredService<IHttpContextAccessor>()

        { render =
            fun plant ->
                (render
                    (currentUser.get
                     |> Option.map (fun user ->
                         {| liked = User.Wants plant.id user
                            has = User.Has plant.id user
                            antiForgeryToken = antiForgery.GetAndStoreTokens httpContextAccessor.HttpContext |}))
                    plant) })
