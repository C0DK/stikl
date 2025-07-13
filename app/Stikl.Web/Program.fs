namespace webapp

open System
open System.Threading
open Auth0.AspNetCore.Authentication
open System.Threading.Tasks
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.OpenIdConnect
open Microsoft.AspNetCore.HttpOverrides
open Microsoft.Extensions.Logging
open domain
open webapp.services
open webapp.services.User

#nowarn "20"

open FSharp.MinimalApi.DI
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open webapp

module EnvironmentVariable =
    let getRequired key =
        // TODO does this fail if empty?
        match Environment.GetEnvironmentVariable key with
        | v when v.Trim() <> "" -> v
        | _ -> failwith $"Env var '{key}' was not set!"

module Program =
    let exitCode = 0


    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        // TODO: this fails on enhanced cookie protection
        // TODO: This is needed because we dont use https probably.
        // Cookie configuration for HTTP to support cookies with SameSite=None
        builder.Services.Configure<CookiePolicyOptions>(fun (options: CookiePolicyOptions) ->
            let CheckSameSite (options: CookieOptions) =
                if (options.SameSite = SameSiteMode.None && options.Secure = false) then
                    options.SameSite <- SameSiteMode.Unspecified

            options.MinimumSameSitePolicy <- SameSiteMode.Unspecified
            options.Secure <- CookieSecurePolicy.None
            options.OnAppendCookie <- fun cookieContext -> CheckSameSite(cookieContext.CookieOptions)
            options.OnDeleteCookie <- fun cookieContext -> CheckSameSite(cookieContext.CookieOptions))
        // Cookie configuration for HTTPS
        //  builder.Services.Configure<CookiePolicyOptions>(fun (options :CookiePolicyOptions) ->
        //     options.MinimumSameSitePolicy <- SameSiteMode.None;
        //  );

        builder.Services.Configure<ForwardedHeadersOptions>(fun (options: ForwardedHeadersOptions) ->
            options.ForwardedHeaders <-
                ForwardedHeaders.XForwardedProto
                ||| ForwardedHeaders.XForwardedHost
                ||| ForwardedHeaders.XForwardedFor)


        builder.Services.AddMemoryCache()
        builder.Services.AddControllers()
        builder.Services.AddHttpContextAccessor()

        builder.Logging.ClearProviders()
        builder.Logging.AddConsole()

        // TODO: move to domain and data access
        builder.Services.AddSingleton<EventHandler>(fun s ->
            let store = s.GetRequiredService<UserStore>()
            let identity = s.GetRequiredService<User.CurrentUser>()
            let eventBroker = s.GetRequiredService<EventBroker.EventBroker>()
            // TODO: use composition variant and move that too.
            { handle =
                (fun event ->
                    match identity with
                    | AuthedUser user ->
                        (UserEvent.create event user.username)
                        |> store.ApplyEvent
                        |> Task.collect (
                            Result.map (fun e ->
                                task {
                                    do! eventBroker.Publish e CancellationToken.None
                                    return e
                                })
                            >> Task.unpackResult
                        )
                    | Anonymous -> Task.FromResult(Error "User")
                    | NewUser _ -> Task.FromResult(Error "Not implemented")) }
            : EventHandler)

        builder.Services
        |> (Composition.registerAll
            >> User.register
            >> EventBroker.register
            >> Components.Htmx.register)

        // Might be needed for APIs
        builder.Services.AddTuples()

        builder.Services
            .AddAuthentication(fun options ->
                options.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
                options.DefaultChallengeScheme <- OpenIdConnectDefaults.AuthenticationScheme)
            .AddCookie()
            .AddOpenIdConnect(fun options ->
                options.ClientId <- Environment.GetEnvironmentVariable("AUTHRESS_CLIENT_ID")
                options.Authority <- Environment.GetEnvironmentVariable("AUTHRESS_LOGIN_URL")
                options.SignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
                options.ResponseType <- "code"
                options.SaveTokens <- true
                options.Scope.Add("openid")
                options.Scope.Add("profile")
                options.UsePkce <- true
                
                options.DisableTelemetry <- true
                options.Events <-
                    OpenIdConnectEvents(
                        OnRedirectToIdentityProvider =
                            fun context ->
                                // remove the parameters that Authress doesnt like.
                                context.ProtocolMessage.Parameters.Remove("nonce")
                                Task.CompletedTask
                    )

            )


        builder.Services.AddAuthorization()
        builder.Services.AddAntiforgery()

        builder.Services.Configure<CookieAuthenticationOptions>(
            CookieAuthenticationDefaults.AuthenticationScheme,
            fun (options: CookieAuthenticationOptions) ->
                // https://stackoverflow.com/a/75716332/3806354
                options.LoginPath <- "/auth/login"
                options.LogoutPath <- "/auth/logout"
        )

        let app = builder.Build()

        let forwardedHeaders = ForwardedHeadersOptions()

        forwardedHeaders.ForwardedHeaders <-
            ForwardedHeaders.XForwardedProto
            ||| ForwardedHeaders.XForwardedHost
            ||| ForwardedHeaders.XForwardedFor

        app
            .UseForwardedHeaders(forwardedHeaders)
            .UseAuthentication()
            .UseAuthorization()
            .UseAntiforgery()

        app.UseMiddleware<RedirectIfAuthedWithoutUser>()

        app.MapControllers()

        app |> routes.Root.apply |> ignore

        app.Run()

        exitCode

// Missing things:
// TODO: ability to request plants / and or message
// TODO: actually datastore
// TODO: Cleanup composition
// TODO: more plants
