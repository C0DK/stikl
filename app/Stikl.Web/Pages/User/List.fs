module Stikl.Web.Pages.User.List

open Stikl.Web.Components
open domain

let render (users: User list) =
    users |> List.map UserCard.render |> Common.grid
