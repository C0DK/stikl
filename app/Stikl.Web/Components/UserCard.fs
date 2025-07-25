module Stikl.Web.Components.UserCard

open Stikl.Web
open domain

let href (user: User) = $"/u/{user.username.value}"

let render (user: domain.User) =
    let locale = Localization.``default``

    // boost fails if re-direct, which the chat does if not authed
    Card.render
        "user"
        {| alt = $"Image of {String.escape user.fullName}"
           src = user.imgUrl |}
        (String.escape user.fullName)
        $"""
        <a 
            href="/chat/{user.username}"
            hx-boost="false"
            class="font-sans text-sm font-bold {Theme.textBrandColor} {Theme.textBrandColorHover} transition"
        >
            {locale.userDetails.chat} <i aria-hidden="true" class="fa-solid fa-message"></i>
        </a>
        <p class="italic text-gray-600">{user.location.location.label}</p>
        """
        (href user)
