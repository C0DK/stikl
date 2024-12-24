namespace webapp

open System.Security.Claims

type Principal =
    { username: string
      firstName: string option
      surname: string option
      email: string option
      img: string option
      claims: Claim list }

module Principal =
    let fromClaims (user: ClaimsPrincipal) : Principal =
        let getClaim t =
            user.Claims |> Seq.tryFind (fun claim -> claim.Type = t) |> Option.map (_.Value)

        // TODO: figure out what to expose if not the sub/subject publically. seems not to be ideal
        { username = user.Identity.Name
          firstName = getClaim ClaimTypes.GivenName
          surname = getClaim ClaimTypes.Surname
          email = getClaim ClaimTypes.Email
          img = getClaim "picture"
          claims = user.Claims |> Seq.toList }

    let tryFromClaims (user: ClaimsPrincipal) : Principal Option =
        match user.Identity with
        | id when id.IsAuthenticated -> Some(fromClaims user)
        | _ -> None

type TryGetIdentity =
    | TryGetIdentity of (unit -> Principal option)

    member this.apply =
        let (TryGetIdentity f) = this
        f

type PrincipalSource =
    { get: unit -> Principal
      tryGet: unit -> Principal option }
