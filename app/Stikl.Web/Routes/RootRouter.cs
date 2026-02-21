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
        LocationRouter.Map(app.MapGroup("/location").RequireAuthorization());
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
                                .Select(s => CreatePlantCard(user, s))
                                .ToArrayAsync()
                        )
                        : ""
                );
                return new PageResult(content, q is null ? "Stikl" : $"Stikl | '{q}' results");
            }
        );
    }

    // TODO: move this, and query, to plant router.
    private static PlantCard CreatePlantCard(User? viewer, Species species)
    {
        var url = $"/plant/{species.Id}";
        return new PlantCard(
            wantButton: viewer?.Wants.Contains(species.Id) is true
                ? new PlantCardUnWantButton(url)
                : new PlantCardWantButton(url),
            commonName: species.CommonName,
            scientificName: species.ScientificName,
            imageSource: species.SmallImage?.ToString()
                ?? "https://easydrawingguides.com/wp-content/uploads/2024/06/how-to-draw-a-plant-featured-image-1200.png",
            url: url,
            WikiLink: "https://en.wikipedia.org/wiki/" + (species.ScientificName.Replace(" ", "_"))
        );
    }
}
