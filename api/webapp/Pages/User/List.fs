module webapp.Pages.User.List

open domain
open webapp
open webapp.services.Htmx

let render (users: User list) (pageBuilder: PageBuilder) =
    users
    |> List.map pageBuilder.userCard
    |> Task.merge
    |> Task.map (Seq.toList >> Components.grid)
