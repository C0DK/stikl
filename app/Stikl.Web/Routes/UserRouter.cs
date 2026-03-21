using Stikl.Web.DataAccess;
using Stikl.Web.Model;
using Stikl.Web.Templates.Components;
using Stikl.Web.Templates.Pages;

namespace Stikl.Web.Routes;

public static class UserRouter
{
    public static void Map(RouteGroupBuilder app)
    {
        app.MapGet(
            "/{username}",
            async ValueTask<IResult> (
                HttpContext context,
                Username username,
                UserSource users,
                SpeciesSource speciesSource,
                CancellationToken cancellationToken
            ) =>
            {
                var user = await users.GetOrNull(username, cancellationToken);
                if (user is null)
                    return new PageResult(new NotFound());
                var principal = await users.GetFromPrincipalOrDefault(
                    context.User,
                    cancellationToken
                );
                return new PageResult(
                    // TODO: strongbars should allow us to say if bio?
                    new UserPage(
                        firstName: user.FirstName,
                        lastName: user.LastName,
                        location: user.Location.DisplayName,
                        hasBio: user.Bio is not null,
                        bio: user.Bio,
                        // TODO: use "Hasplant" esque- card
                        plants: await speciesSource.Get(user.Has.Select(v => v.Id), cancellationToken)
                            .Select(s => PlantRouter.CreatePlantCard(principal, s))
                            .ToArrayAsync()
                    ),
                    $"{user.FirstName} {user.LastName}"
                );
            }
        );
    }
}
