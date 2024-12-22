namespace webapp

open Auth0.AspNetCore.Authentication
open Microsoft.AspNetCore.CookiePolicy

#nowarn "20"

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open webapp.PageBuilder

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
        builder.Services.AddTransient<PageBuilder>()
        builder.Services.AddTransient<UserService>()

        builder.Services.AddAuth0WebAppAuthentication(fun options ->
            options.Domain <- builder.Configuration["Auth0:Domain"]
            options.ClientId <- builder.Configuration["Auth0:ClientId"])

        // TODO handle same sit shit?
        let app = builder.Build()

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.UseAuthentication()
        app.MapControllers()

        app |> Router.routes.Apply |> ignore

        app.Run()

        exitCode
