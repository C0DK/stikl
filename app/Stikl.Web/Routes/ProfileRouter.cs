using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Npgsql;
using Stikl.Web.DataAccess;
using Stikl.Web.Model;
using Stikl.Web.Templates.Components;
using Stikl.Web.Templates.Pages;

namespace Stikl.Web.Routes;

public static class ProfileRouter
{
    public static void Map(IEndpointRouteBuilder builder)
    {
        builder.MapGet(
            "",
            async Task<IResult> (
                ClaimsPrincipal principal,
                NpgsqlDataSource db,
                CancellationToken cancellationToken
            ) =>
            {
                await using var connection = await db.OpenConnectionAsync(cancellationToken);
                var users = new UserSource(connection);
                var user = await users.GetFromPrincipal(principal, cancellationToken);
                return new PageResult(
                    new ProfilePage(
                        nameForm: new ProfileNameForm(
                            firstName: user.FirstName,
                            lastName: user.LastName,
                            errors: new string[] { }
                        ),
                        locationForm: new ProfileLocationForm(
                            selectedLocationName: user.Location.Address.Label
                                ?? user.Location.DisplayName
                        ),
                        bioForm: new ProfileBioForm(bio: user.Bio, errors: new string[] { })
                    ),
                    "Settings"
                );
            }
        );

        builder.MapPost(
            "/name",
            async Task<IResult> (
                HttpContext context,
                ClaimsPrincipal principal,
                NpgsqlDataSource db,
                CancellationToken cancellationToken
            ) =>
            {
                var form = context.Request.Form;
                var errors = new List<FormError>();

                var firstName = form.GetString("firstName")?.Trim();
                if (string.IsNullOrWhiteSpace(firstName))
                    errors.Add(new FormError("First name is required!"));
                var lastName = form.GetString("lastName")?.Trim();
                if (string.IsNullOrWhiteSpace(lastName))
                    errors.Add(new FormError("Last name is required!"));

                if (errors.Count > 0)
                    return new ComponentResult(
                        new ProfileNameForm(
                            firstName: firstName ?? "",
                            lastName: lastName ?? "",
                            errors: errors
                        )
                    );

                await using var connection = await db.OpenConnectionAsync(cancellationToken);
                var users = new UserSource(connection);
                var user = await users.GetFromPrincipal(principal, cancellationToken);

                var eventWriter = new UserEventWriter(connection);
                await eventWriter.Write(
                    user.UserName,
                    new UpdateName(FirstName: firstName!, LastName: lastName!),
                    cancellationToken
                );

                return new ComponentResult(
                    new ProfileNameForm(
                        firstName: firstName!,
                        lastName: lastName!,
                        errors: new string[] { }
                    )
                );
            }
        );

        builder.MapGet(
            "/delete",
            async Task<IResult> (
                ClaimsPrincipal principal,
                NpgsqlDataSource db,
                CancellationToken cancellationToken
            ) =>
            {
                await using var connection = await db.OpenConnectionAsync(cancellationToken);
                var users = new UserSource(connection);
                var user = await users.GetFromPrincipal(principal, cancellationToken);
                return new ModalResult(
                    "Delete Account",
                    new DeleteAccountConfirm(username: user.UserName, errors: new string[] { })
                );
            }
        );

        builder.MapPost(
            "/delete",
            async Task<IResult> (
                HttpContext context,
                ClaimsPrincipal principal,
                NpgsqlDataSource db,
                CancellationToken cancellationToken
            ) =>
            {
                await using var connection = await db.OpenConnectionAsync(cancellationToken);
                var users = new UserSource(connection);
                var user = await users.GetFromPrincipal(principal, cancellationToken);

                var form = context.Request.Form;
                var confirmed = form.GetString("confirmUsername")?.Trim();
                if (confirmed != user.UserName.Value)
                    return new ComponentResult(
                        new DeleteAccountConfirm(
                            username: user.UserName,
                            errors: [new FormError("Username did not match.")]
                        )
                    );

                var eventWriter = new UserEventWriter(connection);
                await eventWriter.Write(user.UserName, new AccountDeleted(), cancellationToken);

                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return new RedirectResult("/");
            }
        );

        builder.MapPost(
            "/bio",
            async Task<IResult> (
                HttpContext context,
                ClaimsPrincipal principal,
                NpgsqlDataSource db,
                CancellationToken cancellationToken
            ) =>
            {
                var form = context.Request.Form;
                var bio = form.GetString("bio")?.Trim();
                if (string.IsNullOrWhiteSpace(bio))
                    bio = null;

                await using var connection = await db.OpenConnectionAsync(cancellationToken);
                var users = new UserSource(connection);
                var user = await users.GetFromPrincipal(principal, cancellationToken);

                var eventWriter = new UserEventWriter(connection);
                await eventWriter.Write(user.UserName, new SetBio(Bio: bio), cancellationToken);

                return new ComponentResult(new ProfileBioForm(bio: bio, errors: new string[] { }));
            }
        );

        builder.MapPost(
            "/location",
            async Task<IResult> (
                HttpContext context,
                ClaimsPrincipal principal,
                LocationIQClient locationIq,
                NpgsqlDataSource db,
                CancellationToken cancellationToken
            ) =>
            {
                var form = context.Request.Form;
                var errors = new List<FormError>();

                var osmId = form.GetString("osmId")?.Trim();

                var location = osmId is { } id ? await locationIq.Get(id, cancellationToken) : null;

                if (location is null)
                    throw new InvalidOperationException("location couldn't be found");

                await using var connection = await db.OpenConnectionAsync(cancellationToken);
                var users = new UserSource(connection);
                var user = await users.GetFromPrincipal(principal, cancellationToken);

                var eventWriter = new UserEventWriter(connection);
                await eventWriter.Write(
                    user.UserName,
                    new UpdateLocation(Location: location!),
                    cancellationToken
                );

                return new ComponentResult(
                    new ProfileLocationForm(selectedLocationName: location.Address.Label)
                );
            }
        );
        builder.MapGet(
            "location/search",
            async (
                LocationIQClient locationIq,
                ILogger logger,
                string locationQuery,
                CancellationToken cancellationToken
            ) =>
            {
                // dont 404 on missing results..
                var suggestions = await locationIq.AutoComplete(locationQuery, cancellationToken);
                logger.ForContext("entries", suggestions, true).Debug("Got suggestions");
                return string.Join(
                    "\n",
                    suggestions.Select(suggestion => new ProfilePageLocationSelector(
                        osmId: suggestion.OsmId,
                        label: suggestion.DisplayPlace,
                        address: suggestion.DisplayName
                    ))
                );
            }
        );
    }
}
