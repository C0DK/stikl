using System.Security.Claims;
using System.Security.Cryptography;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Stikl.Web.Templates.Components;
using Stikl.Web.Templates.Pages;

namespace Stikl.Web.Routes;

public static class AuthRouter
{
    public static void Map(RouteGroupBuilder app)
    {
        app.MapGet(
            "/",
            (HttpContext context, string? redirect = null) =>
            {
                if (context.User.Identity?.IsAuthenticated is true)
                {
                    context.Response.Headers["Hx-Redirect"] = redirect ?? "/";
                    return Results.Redirect(redirect ?? "/");
                }
                return new PageResult(
                    new LoginPage(new LoginForm(email: null, error: null)),
                    "Stikl | Please Sign In"
                );
            }
        );
        app.MapPost(
            "/",
            async (
                HttpContext context,
                NpgsqlDataSource db,
                CancellationToken cancellationToken,
                ILogger logger,
                [FromForm] string email,
                string? redirect = null
            ) =>
            {
                email = email.ToLowerInvariant();
                if (!IsValidEmail(email))
                    // TODO: this wont work due to the target...
                    return RenderForm(
                        new LoginForm(
                            email: email,
                            error: new FormError(message: "Email is not valid!")
                        )
                    );
                await using var connection = await db.OpenConnectionAsync(cancellationToken);

                var code = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
                logger.ForContext("code", code).Warning("Should have sent {code} via email", code);

                await connection.ExecuteAsync(
                    @"INSERT INTO signin_otp(email, code) VALUES(@email, @code)",
                    new { email, code }
                );

                return RenderForm(new OtpCodeField(email: email, error: null));
            }
        );
        app.MapPost(
            "/code",
            async (
                HttpContext context,
                NpgsqlDataSource db,
                CancellationToken cancellationToken,
                [FromForm] string email,
                [FromForm] string code,
                string? redirect = null
            ) =>
            {
                if (!IsValidEmail(email))
                    return RenderForm(
                        new LoginForm(
                            email: email,
                            error: new FormError(message: "Email is not valid!")
                        )
                    );

                // TODO: handle never Remember Me and stuff
                email = email.ToLowerInvariant();
                await using var connection = await db.OpenConnectionAsync(cancellationToken);

                var matches = (
                    await connection.QueryAsync<DateTime>(
                        @"
                    SELECT                      
                      created
                    FROM signin_otp
                    WHERE email = @email AND code = @code 
                    ",
                        new { email, code }
                    )
                ).ToArray();
                if (matches is not { Length: > 0 })
                    return RenderForm(
                        new OtpCodeField(email: email, error: new FormError("Invalid code!"))
                    );
                if (
                    (DateTimeOffset.UtcNow - matches.Single()).Duration() > TimeSpan.FromMinutes(10)
                )
                    return RenderForm(
                        new LoginForm(email: email, error: new FormError("Code expired"))
                    );

                var claimsIdentity = new ClaimsIdentity(
                    [new Claim(ClaimTypes.Email, email)],
                    CookieAuthenticationDefaults.AuthenticationScheme
                );

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity)
                );
                // TODO: add toast?
                // TODO: better actual redirect!
                context.Response.Headers["Hx-Redirect"] = redirect ?? "/";
                return Results.Text("Redirecting??");
            }
        );
        app.MapGet(
            "/logout",
            (HttpContext context, string? redirect = null) =>
            {
                context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect(redirect ?? "/");
            }
        );
    }

    private static IResult RenderForm(string content) =>
        // TODO helper
        Results.Text(content, "text/html", statusCode: 200);

    static bool IsValidEmail(string email)
    {
        var trimmedEmail = email.Trim();

        if (trimmedEmail.EndsWith("."))
        {
            return false; // suggested by @TK-421
        }
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == trimmedEmail;
        }
        catch
        {
            return false;
        }
    }
}
