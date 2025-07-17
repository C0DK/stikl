module Stikl.Web.Components.UserCard

open Stikl.Web

let render (user: domain.User) =
    let locale = Localization.``default``

    Card.render
        "user"
        {| alt = $"Image of {user.fullName}"
           src = user.imgUrl |}
        user.fullName
        $"<a class='cursor-pointer text-sm text-lime-600 underline hover:text-lime-400' href='/user/{user.username.value}'>{locale.seeProfile}</a>"
