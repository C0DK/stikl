namespace webapp

open System.Security.Claims
open Microsoft.AspNetCore.Http

type User =
    { username: string
      firstName: string option
      surname: string option
      email: string option
      img: string option }

module User =
    let FromClaims (user: ClaimsPrincipal) : User =
            let getClaim t =
                user.Claims |> Seq.tryFind (fun claim -> claim.Type = t) |> Option.map (_.Value)

            { username = user.Identity.Name
              firstName = getClaim ClaimTypes.GivenName
              surname = getClaim ClaimTypes.Surname
              email = getClaim ClaimTypes.Email
              img = getClaim "picture" }
            
    let tryFromClaims (user: ClaimsPrincipal) : User Option =
        match user.Identity with
        | id when id.IsAuthenticated ->
            Some (FromClaims user)
        | _ -> None

type TryGetUser =
    | TryGetUser of (unit -> User option)
    
    member this.apply =
        let (TryGetUser f) = this
        f
type UserSource =
    | UserSource of (unit -> User)
    
    member this.get =
        let (UserSource f) = this
        f
        