module webapp.Pages.User.List

open domain
open webapp
open webapp.Components.Htmx

let render (users: User list) (pageBuilder: PageBuilder) =
    users |> List.map pageBuilder.userCard |> Components.Common.grid
