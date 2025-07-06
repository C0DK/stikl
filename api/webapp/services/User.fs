module webapp.services.User

open System.Security.Claims
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open domain

type CurrentUser =
    | AuthedUser of User
    | NewUser of authId: string
    | Anonymous

    member this.get =
        match this with
        | AuthedUser user -> Some user
        | NewUser _ -> None
        | Anonymous -> None

type RedirectIfAuthedWithoutUser(next: RequestDelegate) =
    // TODO: make this less slow and shitty
    member this.InvokeAsync(context: HttpContext, currentUser: CurrentUser) =
        match currentUser with
        | NewUser _ when context.Request.Path.StartsWithSegments("/auth/create") ->
            Results.Redirect("/auth/create").ExecuteAsync(context)
        | _ -> next.Invoke(context)



let register: IServiceCollection -> IServiceCollection =
    Services.registerScoped (fun s ->
        let store = s.GetRequiredService<UserStore>()
        let context = s.GetRequiredService<IHttpContextAccessor>().HttpContext
        let claimsPrincipal = context.User

        let getClaim t =
            claimsPrincipal.Claims
            |> Seq.tryFind (fun claim -> claim.Type = t)
            |> Option.map _.Value

        // TODO cache transient
        match claimsPrincipal.Identity with
        | id when id.IsAuthenticated ->
            let authId = getClaim ClaimTypes.NameIdentifier |> Option.orFail

            (store.GetByAuthId authId)
            |> _.Result
            |> Option.map AuthedUser
            |> Option.defaultValue (NewUser authId)
        | _ -> Anonymous)
