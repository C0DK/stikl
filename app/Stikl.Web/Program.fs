namespace webapp

open System.Threading
open Auth0.AspNetCore.Authentication
open System.Threading.Tasks
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.HttpOverrides
open Microsoft.Extensions.Logging
open Serilog
open Stikl.Web
open Stikl.Web.Pages
open domain
open Stikl.Web.services
open Stikl.Web.services.User

#nowarn "20"

open FSharp.MinimalApi.DI
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting


module Program =
    let exitCode = 0

    let logger = Logging.configure().CreateLogger()
    Log.Logger <- logger

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

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


        builder.Services.AddSerilog()
        builder.Services.AddSingleton(Log.Logger)
        builder.Services.AddMemoryCache()
        builder.Services.AddControllers()
        builder.Services.AddHttpContextAccessor()

        builder.Logging.ClearProviders()
        builder.Logging.AddConsole()


        builder.Services |> Composition.registerAll

        // Might be needed for APIs
        builder.Services.AddTuples()

        builder.Services
            .AddAuth0WebAppAuthentication(fun options ->
                options.Domain <- builder.Configuration["Auth0:Domain"]
                options.ClientId <- builder.Configuration["Auth0:ClientId"]
                options.ClientSecret <- EnvironmentVariable.getRequired "AUTH0_SECRET"
                options.Scope <- "openid profile name email username")
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

        let forwardedHeaders = ForwardedHeadersOptions()

        forwardedHeaders.ForwardedHeaders <-
            ForwardedHeaders.XForwardedProto
            ||| ForwardedHeaders.XForwardedHost
            ||| ForwardedHeaders.XForwardedFor

        app.UseMiddleware<HtmxErrorHandlingMiddleware>()
        app.UseStaticFiles()

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