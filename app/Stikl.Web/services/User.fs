module Stikl.Web.services.User

open System.Security.Claims
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open domain
open Serilog

type CurrentUser =
    | AuthedUser of User
    | NewUser of authId: string
    | Anonymous

    member this.get =
        match this with
        | AuthedUser user -> Some user
        | NewUser _ -> None
        | Anonymous -> None

type RedirectIfAuthedWithoutUser(next: RequestDelegate, logger: ILogger) =
    member this.InvokeAsync(context: HttpContext, currentUser: CurrentUser) =
        let logger = logger.ForContext("user", currentUser)
        let isAuthCreateRequest = context.Request.Path.StartsWithSegments("/auth/create")
        let isLocationEndpoint = context.Request.Path.StartsWithSegments("/location")

        match currentUser with
        | NewUser _ when not (isAuthCreateRequest || isLocationEndpoint) ->
            Results.Redirect("/auth/create").ExecuteAsync(context)
        | AuthedUser _ when isAuthCreateRequest ->
            logger.Information("You cannot go to auth if you arent a new user!")
            Results.Redirect("/").ExecuteAsync(context)
        | _ -> next.Invoke(context)



let register: IServiceCollection -> IServiceCollection =
    Services.registerTransient (fun s ->
        let store = s.GetRequiredService<UserStore>()
        let context = s.GetRequiredService<IHttpContextAccessor>().HttpContext
        let claimsPrincipal = context.User

        let getClaim t =
            claimsPrincipal.Claims
            |> Seq.tryFind (fun claim -> claim.Type = t)
            |> Option.map _.Value

        match claimsPrincipal.Identity with
        | id when id.IsAuthenticated ->
            let authId = getClaim ClaimTypes.NameIdentifier |> Option.orFail

            (store.GetByAuthId authId context.RequestAborted)
            |> _.Result
            |> Option.map AuthedUser
            |> Option.defaultValue (NewUser authId)
        | _ -> Anonymous)
