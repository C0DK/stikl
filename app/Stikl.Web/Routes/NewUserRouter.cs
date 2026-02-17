using System.Security.Claims;
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
                            selectedLocationName: null,
                            errors: new string[] { }
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
                LocationIQClient locationIq,
                CancellationToken cancellationToken,
                NpgsqlDataSource db,
                string? redirect = null
            ) =>
            {
                var form = context.Request.Form;
                // csrf is weird??
                var errors = new List<FormError>();

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
                var osmId = form.GetString("osmId")?.Trim();
                if (string.IsNullOrWhiteSpace(osmId))
                    errors.Add(new FormError("location is required!"));

                // TODO: catch!
                var location = osmId is { } id ? await locationIq.Get(id, cancellationToken) : null;
                if (errors.Count > 0)
                    return new ComponentResult(
                        new CreateUserForm(
                            userName: username,
                            firstName: firstName,
                            lastName: lastName,
                            selectedLocationName: SelectedLocation(location),
                            errors: errors
                        )
                    );
                await using var connection = await db.OpenConnectionAsync(cancellationToken);

                var payload = new UserCreated(
                    Email: principal.GetEmail(),
                    FirstName: firstName!,
                    LastName: lastName!,
                    Location: location!
                );

                var eventWriter = new UserEventWriter(connection);
                try
                {
                    await eventWriter.Write(username, 1, payload, cancellationToken);
                }
                catch (UserEventWriter.EventBrokeConstraint)
                {
                    return new ComponentResult(
                        new CreateUserForm(
                            userName: username,
                            firstName: firstName,
                            lastName: lastName,
                            selectedLocationName: SelectedLocation(location),
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

    private static LocationSelection? SelectedLocation(LocationIQClient.Location? location) =>
        location is not null
            ? new LocationSelection(
                osmId: location.OsmId,
                label: location.Address.Label ?? location.DisplayName,
                address: location.DisplayName
            )
            : null;
}
