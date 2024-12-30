namespace webapp

open System
open Auth0.AspNetCore.Authentication
open Auth0.ManagementApi
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.Extensions.Logging
open webapp.services

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
            new ManagementApiClient(EnvironmentVariable.getRequired "AUTH0_TOKEN", uri))

        // TODO: move to domain

        builder.Services.AddSingleton<routes.Trigger.EventHandler>(fun s ->
            let repo = s.GetRequiredService<Composition.UserStore>()
            // TODO clean up a bit mby?
            { handle = repo.applyEvent }: routes.Trigger.EventHandler)

        builder.Services
        |> Composition.registerAll
        |> User.register
        |> Principal.register
        |> Htmx.register
        
        // Might be needed for APIs
        builder.Services.AddTuples()

        builder.Services.AddAuth0WebAppAuthentication(fun options ->
            options.Domain <- builder.Configuration["Auth0:Domain"]
            options.ClientId <- builder.Configuration["Auth0:ClientId"]
            // TODO: get full scope i.e family name etc?h
            options.Scope <- "openid profile email username")

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
