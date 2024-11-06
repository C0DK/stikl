module api.CurrentUser

open System.Security.Claims
open Microsoft.AspNetCore.Mvc

let get (this: ControllerBase) =
    let claim =
        this.User.Claims
        |> Seq.find (fun claim -> claim.Type = ClaimTypes.NameIdentifier)

    claim.Value |> domain.UserId
