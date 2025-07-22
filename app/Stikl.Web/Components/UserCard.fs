module Stikl.Web.Components.UserCard

open Stikl.Web
open domain

let href (user: User) = $"/u/{user.username.value}"

let render (user: domain.User) =
    let locale = Localization.``default``

    Card.render
        "user"
        {| alt = $"Image of {String.escape user.fullName}"
           src = user.imgUrl |}
        (String.escape user.fullName)
        $"<span class=\"italic text-gray-600\">{user.location.location.label}</span>"
        (href user)
        
