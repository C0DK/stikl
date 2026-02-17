using Npgsql;
using Stikl.Web.DataAccess;
using Stikl.Web.Model;

namespace Stikl.Web.Routes;

public static class PlantRouter
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/{id}/want",
                async (
                    HttpContext context,
                    PlantSearcher searcher,
                    NpgsqlDataSource db,
                    CancellationToken cancellationToken,
                    PlantId id
                ) =>
                {
                    await using var connection = await db.OpenConnectionAsync(cancellationToken);
                    var writer = new UserEventWriter(connection);

                    // TODO: redirect to signup if no existo?
                    var username = context.User.GetUsername();
                    await writer.Write(username, new WantPlant(id), cancellationToken);

                    // TODO: updated card!
                    return Results.Ok();
                }
            // TODO: on redirect do a toast!
            )
            .RequireAuthorization(); // require signup to be done!
    }
}
