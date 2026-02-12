using System.Security.Claims;
using System.Security.Cryptography;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Npgsql;
using Stikl.Web.DataAccess;
using Stikl.Web.Model;
using Stikl.Web.Templates.Components;
using Stikl.Web.Templates.Pages;

namespace Stikl.Web.Routes;

public static class AuthRouter
{
    public static void Map(RouteGroupBuilder app)
    {
        app.MapGet(
            "/",
            IResult (HttpContext context, string? redirect = null) =>
            {
                if (context.User.Identity?.IsAuthenticated is true)
                    return new RedirectResult(redirect ?? "/");

                return new PageResult(
                    new LoginPage(new LoginForm(email: null, error: null)),
                    "Stikl | Please Sign In"
                );
            }
        );
        app.MapPost(
            "/",
            async (
                HttpRequest request,
                NpgsqlDataSource db,
                CancellationToken cancellationToken,
                ILogger logger,
                string? redirect = null
            ) =>
            {
                if (!Email.TryParse(request.Form.GetString("email"), out var email))
                    return RenderLoginForm(email: email, error: "Email is not valid!");

                await using var connection = await db.OpenConnectionAsync(cancellationToken);

                var code = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
                logger.ForContext("code", code).Warning("Should have sent {code} via email", code);

                await connection.ExecuteAsync(
                    @"INSERT INTO signin_otp(email, code) VALUES(@email, @code)",
                    // TODO: make dapper handle the email better or maybe dont use dapper
                    new { email = email.Value, code }
                );

                return RenderCodeForm(email);
            }
        );
        app.MapPost(
            "/code",
            async (
                HttpContext context,
                NpgsqlDataSource db,
                CancellationToken cancellationToken,
                string? redirect = null
            ) =>
            {
                var form = context.Request.Form;
                if (!Email.TryParse(form.GetString("email"), out var email))
                    return RenderLoginForm(email: email, error: "Email is not valid!");
                if (form.GetString("code") is not { Length: > 0 } code)
                    return RenderCodeForm(email, "Invalid code!");

                // TODO: handle never Remember Me and stuff
                await using var connection = await db.OpenConnectionAsync(cancellationToken);

                var matches = (
                    await connection.QueryAsync<DateTime>(
                        @"
                    SELECT                      
                      created
                    FROM signin_otp
                    WHERE email = @email AND code = @code 
                    ",
                        new { email = email.Value, code }
                    )
                ).ToArray();
                if (matches is not { Length: > 0 })
                    return RenderCodeForm(email, "Invalid code!");
                if (DateTimeOffset.UtcNow.Subtract(matches.Single()) > TimeSpan.FromMinutes(10))
                    return RenderLoginForm(email: email, error: "Code expired");

                // TODO: claim for username etc etc from database if exists. else redirect.
                var userStore = new UserSource(connection);
                var user = await userStore.GetOrNull(email, cancellationToken);
                if (user is null)
                {
                    await SignIn(context, [new Claim(ClaimTypes.Email, email)]);
                    return new RedirectResult("/auth/new");
                }
                else
                {
                    await SignIn(context, user);
                    // TODO: add toast?
                    return new RedirectResult(redirect ?? "/");
                }
            }
        );
        app.MapGet(
            "/logout",
            (HttpContext context, string? redirect = null) =>
            {
                context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect(redirect ?? "/");
            }
        // should we check if authed? will it fail if not?
        );
    }

    public static async ValueTask SignIn(HttpContext context, User user) =>
        await SignIn(
            context,
            [
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Name, user.UserName),
            ]
        );

    private static async ValueTask SignIn(HttpContext context, IEnumerable<Claim> claims)
    {
        var claimsIdentity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity)
        );
    }

    private static IResult RenderCodeForm(Email email) =>
        new ComponentResult(new OtpCodeField(email: email, error: null));

    private static IResult RenderCodeForm(Email email, string error) =>
        new ComponentResult(new OtpCodeField(email: email, error: new FormError(error)));

    private static IResult RenderLoginForm(Email email) =>
        new ComponentResult(new LoginForm(email: email, error: null));

    private static IResult RenderLoginForm(Email email, string error) =>
        new ComponentResult(new LoginForm(email: email.Value, error: new FormError(error)));
}
