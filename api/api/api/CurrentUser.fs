module api.CurrentUser

open System.Security.Claims
open Microsoft.AspNetCore.Mvc

let get (this: ControllerBase) =
    let claimIsName (claim: Claim) = claim.Type = ClaimTypes.NameIdentifier

    match this.User.Claims |> Seq.tryFind claimIsName with
    | Some claim -> claim.Value |> domain.UserId |> Ok
    | None ->
        let claimNames = this.User.Claims |> Seq.map (_.Type) |> Seq.toList
        Error(HttpError.BadRequest $"User did not have claim of type Name had {claimNames}")
