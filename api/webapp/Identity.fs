namespace webapp

open System.Security.Claims

type Identity =
    { username: string
      firstName: string option
      surname: string option
      email: string option
      img: string option
      claims: Claim list }

module Identity =
    let FromClaims (user: ClaimsPrincipal) : Identity =
        let getClaim t =
            user.Claims |> Seq.tryFind (fun claim -> claim.Type = t) |> Option.map (_.Value)

        { username = user.Identity.Name
          firstName = getClaim ClaimTypes.GivenName
          surname = getClaim ClaimTypes.Surname
          email = getClaim ClaimTypes.Email
          img = getClaim "picture"
          claims = user.Claims |> Seq.toList }

    let tryFromClaims (user: ClaimsPrincipal) : Identity Option =
        match user.Identity with
        | id when id.IsAuthenticated -> Some(FromClaims user)
        | _ -> None

type TryGetIdentity =
    | TryGetIdentity of (unit -> Identity option)

    member this.apply =
        let (TryGetIdentity f) = this
        f

type IdentitySource =
    | IdentitySource of (unit -> Identity)

    member this.get =
        let (IdentitySource f) = this
        f
