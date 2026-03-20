using Npgsql;
using Stikl.Web.DataAccess;
using Stikl.Web.Model;
using Stikl.Web.Templates.Components;
using Stikl.Web.Templates.Pages;

namespace Stikl.Web.Routes;

public static class RootRouter
{
    public static void Map(WebApplication app)
    {
        AuthRouter.Map(app.MapGroup("/auth/"));
        // TODO: RequireAuthorization should check whether user is "done". claim?
        NewUserRouter.Map(app.MapGroup("/auth/new").RequireAuthorization());
        ChatRouter.Map(app.MapGroup("/chat/").RequireAuthorization());
        UserRouter.Map(app.MapGroup("/u/"));
        LocationRouter.Map(app.MapGroup("/location").RequireAuthorization());
        ProfileRouter.Map(app.MapGroup("/profile").RequireAuthorization());
        PlantRouter.Map(app.MapGroup("/plant"));
        app.MapGet(
            "/",
            async (
                HttpContext context,
                NpgsqlDataSource db, // TODO: can we get the connection parts somewhat better from like an transient requirement?
                PlantSearcher searcher,
                CancellationToken cancellationToken,
                string? q = null
            ) =>
            {
                await using var connection = await db.OpenConnectionAsync(cancellationToken);
                var users = new UserSource(connection);
                var user = await users.GetFromPrincipalOrDefault(context.User, cancellationToken);
                var content = new IndexPage(
                    searchBlock: new Search(query: q),
                    searchResult: !string.IsNullOrWhiteSpace(q)
                        ? new SearchResults(
                            await searcher
                                .GetSearchResults(q, cancellationToken)
                                .Select(s => PlantRouter.CreatePlantCard(user, s))
                                .ToArrayAsync()
                        )
                        : ""
                );
                return new PageResult(content, q is null ? "Stikl" : $"Stikl | '{q}' results");
            }
        );
    }
}
