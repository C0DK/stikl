module Stikl.Web.Pages.User.List

open domain
open Stikl.Web
open Stikl.Web.Components.Htmx

let render (users: User list) (pageBuilder: PageBuilder) =
    users |> List.map pageBuilder.userCard |> Components.Common.grid
