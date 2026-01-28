using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Npgsql;
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
                var claimsIdentity = new ClaimsIdentity(
                    [new Claim(ClaimTypes.Email, email)],
                    CookieAuthenticationDefaults.AuthenticationScheme
                );

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity)
                );
                // TODO: add toast?
                return new RedirectResult(redirect ?? "/");
            }
        );
        app.MapGet(
                "/logout",
                (HttpContext context, string? redirect = null) =>
                {
                    context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return Results.Redirect(redirect ?? "/");
                }
            )
            .RequireAuthorization();

        // TODO: RequireAuthorization should check whether user is "done" or nah. claim?
        var newUser = app.MapGroup("/new/").RequireAuthorization();
        newUser.MapGet(
            "",
            (HttpContext context, string? redirect = null) =>
            {
                // TODO: check if user already has user.
                return new PageResult(
                    new CreateUserPage(
                        // TODO: add location suggestions from javascript?
                        new CreateUserForm(
                            userName: null,
                            firstName: null,
                            lastName: null,
                            location: null,
                            locationSuggestions: null,
                            errors: null
                        )
                    ),
                    "Stikl | Sign Up"
                );
            }
        );
        newUser.MapPost(
            "",
            (HttpContext context, string? redirect = null) =>
            {
                var errors = new List<string>();

                var form = context.Request.Form;
                var userName = form.GetString("userName")?.ToLowerInvariant()?.Trim();
                if (string.IsNullOrWhiteSpace(userName)) // lower
                    errors.Add(new FormError("Username is required!"));
                if (userName is not null && !Regex.IsMatch(userName, @"^[a-zA-Z][\w\d_]*$"))
                    // TODO: additional username validation
                    errors.Add(
                        new FormError(
                            "Username can only contains letters, numbers and underscores required!"
                        )
                    );
                var firstName = form.GetString("firstName")?.Trim();
                if (string.IsNullOrWhiteSpace(firstName))
                    errors.Add(new FormError("First name is required!"));
                var lastName = form.GetString("lastName")?.Trim();
                if (string.IsNullOrWhiteSpace(lastName))
                    errors.Add(new FormError("Last name is required!"));
                var location = form.GetString("location")?.Trim();
                if (string.IsNullOrWhiteSpace(location))
                    errors.Add(new FormError("location is required!"));

                if (errors.Count > 0)
                    return new ComponentResult(
                        new CreateUserForm(
                            userName: userName,
                            firstName: firstName,
                            lastName: lastName,
                            location: location,
                            locationSuggestions: null,
                            errors: errors.ToArray()
                        )
                    );

                throw new NotImplementedException();
            }
        );
    }

    private static IResult RenderCodeForm(Email email) =>
        new ComponentResult(new OtpCodeField(email: email, error: null));

    private static IResult RenderCodeForm(Email email, string error) =>
        new ComponentResult(new OtpCodeField(email: email, error: new FormError(error)));

    private static IResult RenderLoginForm(Email email) =>
        new ComponentResult(new LoginForm(email: email, error: null));

    private static IResult RenderLoginForm(Email email, string error) =>
        new ComponentResult(new LoginForm(email: email, error: new FormError(error)));
}

public static class FormExtensions
{
    public static string? GetString(this IFormCollection form, string key)
    {
        if (form.TryGetValue(key, out var value))
            return value.SingleOrDefault();

        return null;
    }
}
