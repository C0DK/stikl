namespace webapp

open System
open Auth0.AspNetCore.Authentication
open Auth0.ManagementApi
open Auth0
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Diagnostics
open Microsoft.AspNetCore.Identity
open webapp.Page

#nowarn "20"

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open webapp.Page
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

        builder.Services.AddControllers()
        builder.Services.AddHttpContextAccessor()

        builder.Services.AddTransient<IdentitySource>(fun s ->
            let httpContextAccessor = s.GetRequiredService<IHttpContextAccessor>()

            { get = (fun () -> Identity.fromClaims httpContextAccessor.HttpContext.User)
              tryGet = (fun () -> Identity.tryFromClaims httpContextAccessor.HttpContext.User) })


        printfn "URI = %s" builder.Configuration["Auth0:Domain"]
        let uri = Uri("https://" + builder.Configuration["Auth0:Domain"] + "/api/v2")
        printfn $"URL = {uri}"
        // TODO how do we dispose?
        builder.Services.AddSingleton<ManagementApiClient>(fun s ->
            new ManagementApiClient(EnvironmentVariable.getRequired "AUTH0_TOKEN", uri))

        builder.Services.AddSingleton<IdentityClient>(fun s ->
            let client = s.GetRequiredService<ManagementApiClient>()

            { getUser = getIdentity client
              listUsers = listUsers client })

        builder.Services.AddTransient<PageBuilder>(fun s ->
            let identitySource = s.GetRequiredService<IdentitySource>()

            { toPage = fun content -> (renderPage content (identitySource.tryGet ())) })

        builder.Services.AddAuth0WebAppAuthentication(fun options ->
            options.Domain <- builder.Configuration["Auth0:Domain"]
            options.ClientId <- builder.Configuration["Auth0:ClientId"]
            options.Scope <- "openid profile email username")

        builder.Services.AddAuthorization()

        builder.Services.Configure<CookieAuthenticationOptions>(
            CookieAuthenticationDefaults.AuthenticationScheme,
            fun (options: CookieAuthenticationOptions) ->
                // https://stackoverflow.com/a/75716332/3806354
                options.LoginPath <- "/auth/login"
                options.LogoutPath <- "/auth/logout"
        )

        let app = builder.Build()

        app.UseHttpsRedirection()

        app.UseAuthentication()
        app.UseAuthorization()
        app.UseDeveloperExceptionPage()
        app.UseExceptionHandler("/epic_fail")
        app.MapControllers()

        app |> routes.Root.apply |> ignore

        app.Run()

        exitCode
