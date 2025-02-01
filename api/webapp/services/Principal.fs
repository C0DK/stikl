namespace webapp.services

open System.Security.Claims
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection

type Claim = { key: string; value: string }

type Principal =
    { auth0Id: string
      username: domain.Username
      firstName: string option
      surname: string option
      email: string option
      img: string option
      claims: Claim list }

module Principal =

    let fromClaims (claimsPrincipal: ClaimsPrincipal) : Principal =
        let getClaim t =
            claimsPrincipal.Claims
            |> Seq.tryFind (fun claim -> claim.Type = t)
            |> Option.map _.Value

        // TODO: we should use the principal less, and simply rely on the user object. It's better, and less buggy
        { auth0Id = getClaim ClaimTypes.NameIdentifier |> Option.orFail
          // TODO: this is not the actual username, but a human readable name - we need to get that too.
          username = domain.Username claimsPrincipal.Identity.Name
          firstName = getClaim ClaimTypes.GivenName
          surname = getClaim ClaimTypes.Surname
          email = getClaim ClaimTypes.Email
          img = getClaim "picture"
          claims =
            claimsPrincipal.Claims
            |> Seq.map (fun c -> { key = c.Type; value = c.Value })
            |> Seq.toList }

    let tryFromClaims (user: ClaimsPrincipal) : Principal Option =
        match user.Identity with
        | id when id.IsAuthenticated -> Some(fromClaims user)
        | _ -> None

    let register: IServiceCollection -> IServiceCollection =
        Services.registerScoped (fun s ->
            let httpContextAccessor = s.GetRequiredService<IHttpContextAccessor>()

            let getPrincipal () =
                tryFromClaims httpContextAccessor.HttpContext.User

            getPrincipal)
        >> Services.registerScoped (fun s ->
            let httpContextAccessor = s.GetRequiredService<IHttpContextAccessor>()
            tryFromClaims httpContextAccessor.HttpContext.User)
        >> Services.registerScoped (fun s ->
            let httpContextAccessor = s.GetRequiredService<IHttpContextAccessor>()
            fromClaims httpContextAccessor.HttpContext.User)
