namespace webapp

open System
open Auth0.AspNetCore.Authentication
open Auth0.ManagementApi
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.Extensions.Logging
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

        builder.Services.AddEndpointsApiExplorer().AddSwaggerGen()

        builder.Services.AddMemoryCache()
        builder.Services.AddControllers()
        builder.Services.AddHttpContextAccessor()

        builder.Logging.ClearProviders()
        builder.Logging.AddConsole()
        printfn "URI = %s" builder.Configuration["Auth0:Domain"]
        let uri = Uri("https://" + builder.Configuration["Auth0:Domain"] + "/api/v2")
        printfn $"URL = {uri}"

        // TODO how do we dispose?
        // todo https://github.com/auth0/auth0.net/issues/171
        builder.Services.AddSingleton<ManagementApiClient>(fun s ->
            let token = EnvironmentVariable.getRequired "AUTH0_TOKEN"
            // TODO: Get the access token,
            new ManagementApiClient(token, uri))

        // TODO: move to domain and data access
        builder.Services.AddSingleton<routes.Trigger.EventHandler>(fun s ->
            let store = s.GetRequiredService<Composition.UserStore>()
            let users = s.GetRequiredService<UserSource>()
            // TODO: use composition variant and move that too.
            { handle =
                (fun event ->
                    users.getFromPrincipal ()
                    |> Task.collect (Option.orFail >> _.username >> store.applyEvent event)) }
            : routes.Trigger.EventHandler)

        builder.Services
        |> (Composition.registerAll >> User.register >> Principal.register >> Htmx.register >> Auth0.register)

        // Might be needed for APIs
        builder.Services.AddTuples()

        builder.Services
            .AddAuth0WebAppAuthentication(fun options ->
                options.Domain <- builder.Configuration["Auth0:Domain"]
                options.ClientId <- builder.Configuration["Auth0:ClientId"]
                options.ClientSecret <- EnvironmentVariable.getRequired "AUTH0_SECRET"
                // TODO: when we refactor principal - we should limit scopes as we dont use them as the are dumb
                options.Scope <- "openid profile name email username"
            )
            .WithAccessToken(fun options ->
                options.Audience <- builder.Configuration["Auth0:Audience"]
                options.UseRefreshTokens <- true)

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

        app
            .UseHttpsRedirection()
            .UseSwagger()
            .UseSwaggerUI()
            .UseAuthentication()
            .UseAuthorization()
            .UseDeveloperExceptionPage()
            .UseAntiforgery()

        app.MapControllers()

        app |> routes.Root.apply |> ignore

        app.Run()

        exitCode

// Missing things:
// TODO: ability to request plants / and or message
// TODO: actually datastore
// TODO: event store
// TODO: Cleanup composition
// TODO: more plants