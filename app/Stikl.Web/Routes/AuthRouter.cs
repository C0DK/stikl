using System.Security.Claims;
using System.Security.Cryptography;
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
            IResult (HttpContext context, string? returnUrl = null) =>
            {
                if (context.User.Identity?.IsAuthenticated is true)
                    return new RedirectResult(returnUrl ?? "/");

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
                string? returnUrl = null
            ) =>
            {
                if (!Email.TryParse(request.Form.GetString("email"), out var email))
                    return RenderLoginForm(email: email, error: "Email is not valid!");

                await using var connection = await db.OpenConnectionAsync(cancellationToken);

                var code = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
                logger.ForContext("code", code).Warning("Should have sent {code} via email", code);

                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO signin_otp(email, code) VALUES($1, $2)",
                    connection
                )
                {
                    Parameters = { NpgsqlParam.Create(email), NpgsqlParam.Create(code) },
                };
                await cmd.ExecuteNonQueryAsync();

                return new PageResult(
                    new OtpCodeField(email: email, error: null),
                    "Stikl | Email code"
                );
            }
        );
        app.MapPost(
            "/code",
            async (
                HttpContext context,
                NpgsqlDataSource db,
                ToastHandler toast,
                CancellationToken cancellationToken,
                string? returnUrl = null
            ) =>
            {
                var form = context.Request.Form;
                if (!Email.TryParse(form.GetString("email"), out var email))
                    return RenderLoginForm(email: email, error: "Email is not valid!");
                if (form.GetString("code") is not { Length: > 0 } code)
                    return RenderCodeForm(email, "Invalid code!");

                // TODO: handle never Remember Me and stuff
                await using var connection = await db.OpenConnectionAsync(cancellationToken);

                await using var cmd = new NpgsqlCommand(
                    @"
                    SELECT                      
                      created
                    FROM signin_otp
                    WHERE email = $1 AND code = $2 
                    ",
                    connection
                )
                {
                    Parameters = { NpgsqlParam.Create(email), NpgsqlParam.Create(code) },
                };
                var matches = await cmd.ReadAllAsync(
                        reader => reader.GetFieldValue<DateTime>(0),
                        cancellationToken
                    )
                    .ToArrayAsync();
                if (matches is not { Length: > 0 })
                    return RenderCodeForm(email, "Invalid code!");
                if (DateTimeOffset.UtcNow.Subtract(matches.Single()) > TimeSpan.FromMinutes(10))
                    return RenderLoginForm(email: email, error: "Code expired");

                // TODO: claim for username etc etc from database if exists. else returnUrl.
                var userStore = new UserSource(connection);
                var user = await userStore.GetOrNull(email, cancellationToken);
                if (user is null)
                {
                    await SignIn(context, [new Claim(ClaimTypes.Email, email)]);
                    return NewUserRouter.BlankForm();
                }
                else
                {
                    await SignIn(context, user);
                    toast.Add("Welcome back!", "You have successfully been signed in");
                    return new RedirectResult(returnUrl ?? "/");
                }
            }
        );
        app.MapGet(
            "/logout",
            (HttpContext context, string? returnUrl = null) =>
            {
                context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect(returnUrl ?? "/");
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
        new ComponentResult(new OtpCodeField(email: email, error: error));

    private static IResult RenderLoginForm(Email email) =>
        new ComponentResult(new LoginForm(email: email, error: null));

    private static IResult RenderLoginForm(Email email, string error) =>
        new ComponentResult(new LoginForm(email: email.Value, error: error));
}
