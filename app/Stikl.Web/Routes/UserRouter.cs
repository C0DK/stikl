using Npgsql;
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
                Npgsql.NpgsqlDataSource db,
                CancellationToken cancellationToken
            ) =>
            {
                await using var connection = await db.OpenConnectionAsync(cancellationToken);
                var users = new UserSource(connection);
                var user = await users.GetOrNull(username, cancellationToken);
                if (user is null)
                    return new PageResult(new NotFound());
                var principal = await users.GetFromPrincipalOrDefault(
                    context.User,
                    cancellationToken
                );
                using var plantCommand = new NpgsqlCommand(
                    @"
SELECT 
  perenual_id,
  common_name,
  scientific_name,
  family,
  genus
FROM perenual_species
WHERE perenual_id = ANY($1)
",
                    connection
                )
                {
                    Parameters = { NpgsqlParam.Create(user.Has.Select(i => i.Id.Value).ToArray()) },
                };

                var plants = plantCommand.ReadAllAsync(
                    reader => new Species(
                        Id: new SpeciesId(reader.GetFieldValue<int>(0)),
                        CommonName: reader.GetFieldValue<string>(1),
                        ScientificName: string.Join(" ", reader.GetFieldValue<string[]>(2)),
                        Family: reader.GetStringOrNull(3),
                        Genus: reader.GetStringOrNull(4)
                    ),
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
                        plants: await plants
                            .Select(s => PlantRouter.CreatePlantCard(principal, s))
                            .ToArrayAsync()
                    ),
                    $"Stikl | {user.FirstName} {user.LastName}"
                );
            }
        );
    }
}
