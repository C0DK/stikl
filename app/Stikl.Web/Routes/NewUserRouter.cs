using System.Security.Claims;
using Dapper;
using Npgsql;
using Stikl.Web.DataAccess;
using Stikl.Web.Model;
using Stikl.Web.Templates.Components;
using Stikl.Web.Templates.Pages;

namespace Stikl.Web.Routes;

public class NewUserRouter
{
    public static void Map(IEndpointRouteBuilder builder)
    {
        builder.MapGet(
            "",
            (string? redirect = null) =>
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
        builder.MapPost(
            "",
            async Task<IResult> (
                HttpContext context,
                ClaimsPrincipal principal,
                CancellationToken cancellationToken,
                NpgsqlDataSource db,
                string? redirect = null
            ) =>
            {
                var form = context.Request.Form;
                // csrf is weird??
                var errors = new List<string>();

                Username username;
                if (
                    !Username.TryParse(
                        form.GetString("userName")?.ToLowerInvariant()?.Trim(),
                        out username
                    )
                )
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
                            userName: username,
                            firstName: firstName,
                            lastName: lastName,
                            location: location,
                            locationSuggestions: null,
                            errors: errors.ToArray()
                        )
                    );
                await using var connection = await db.OpenConnectionAsync(cancellationToken);

                var payload = new UserCreated(
                    Email: principal.GetEmail(),
                    FirstName: firstName!,
                    LastName: lastName!,
                    LocationLabel: location! // TODO: get actual location coordinates or something..
                );

                try
                {
                    // TODO: should i have a kind column or nah? experiences differ.
                    // TODO: should username be on event or email? or both?
                    await connection.ExecuteAsync(
                        @"INSERT INTO stikl.user_event(username, version, kind, payload) VALUES(@user, 1, @kind, CAST(@payload AS JSONB))",
                        // TODO: make dapper handle the email better or maybe dont use dapper
                        // TODO: common event serializer!
                        new
                        {
                            user = username.Value,
                            Kind = UserCreated.Kind,
                            payload = payload.Serialize(),
                        }
                    );
                }
                catch (PostgresException exception) when (exception.ConstraintName is not null)
                {
                    return new ComponentResult(
                        new CreateUserForm(
                            userName: username,
                            firstName: firstName,
                            lastName: lastName,
                            location: location,
                            locationSuggestions: null,
                            errors: [new FormError("Username already taken!")]
                        )
                    );
                }
                var userStore = new UserSource(connection);
                var user = await userStore.Refresh(username, cancellationToken);

                // TODO: dont have this dependency and potentially give it with user?
                await AuthRouter.SignIn(context, user);

                // TODO: update claim
                return new RedirectResult("/");
            }
        );
    }
}
