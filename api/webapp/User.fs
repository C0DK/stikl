namespace webapp

open System.Security.Claims
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open Auth0.AspNetCore.Authentication

type User =
    { username: string
      firstName: string option
      surname: string option
      email: string option
      img: string option }

module User =
    let fromClaims (user: ClaimsPrincipal) : User Option =
        match user.Identity with
        | id when id.IsAuthenticated ->
            let getClaim t =
                user.Claims |> Seq.tryFind (fun claim -> claim.Type = t) |> Option.map (_.Value)

            Some
                { username = user.Identity.Name
                  firstName = getClaim ClaimTypes.GivenName
                  surname = getClaim ClaimTypes.Surname
                  email = getClaim ClaimTypes.Email
                  img = getClaim "picture" }
        | _ -> None

type UserService(httpContextAccessor: IHttpContextAccessor) =
    member this.GetUser() =
        User.fromClaims httpContextAccessor.HttpContext.User
