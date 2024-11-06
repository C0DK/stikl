module api.CurrentUser

open System.Security.Claims
open Microsoft.AspNetCore.Mvc

let get (this: ControllerBase) =
    let claimOption =
        this.User.Claims
        |> Seq.tryFind (fun claim -> claim.Type = ClaimTypes.NameIdentifier)

    match claimOption with
    | Some claim -> claim.Value |> domain.UserId |> Ok
    | None ->
        let claimNames = this.User.Claims |> Seq.map (_.Type) |> Seq.toList
        Error $"User did not have claim of type Name had {claimNames}"
