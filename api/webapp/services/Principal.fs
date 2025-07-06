namespace webapp.services

open System.Security.Claims
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection

type Claim = { key: string; value: string }

type Principal =
    { auth0Id: string
      username: domain.Username
      claims: Claim list }
    

// TODO move the UserOfPrincipal stuff here

module Principal =

    let fromClaims (claimsPrincipal: ClaimsPrincipal) : Principal =
        let getClaim t =
            claimsPrincipal.Claims
            |> Seq.tryFind (fun claim -> claim.Type = t)
            |> Option.map _.Value

        // TODO: we should use the principal less, and simply rely on the user object. It's better, and less buggy
        { auth0Id = getClaim ClaimTypes.NameIdentifier |> Option.orFail
          username = domain.Username claimsPrincipal.Identity.Name
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
