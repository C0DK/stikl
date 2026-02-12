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
        app.MapGet(
            "/",
            async (
                HttpContext context,
                PlantSearcher searcher,
                CancellationToken cancellationToken,
                string? q = null
            ) =>
            {
                var content = new IndexPage(
                    searchBlock: new Search(query: q),
                    searchResult: !string.IsNullOrWhiteSpace(q)
                        ? new SearchResults(
                            results: await searcher
                                .GetSearchResults(q, cancellationToken)
                                .Select(Species.ToPlantCard)
                                .Cast<string>()
                                // TODO: strongbars array should not require ToString here..!
                                .ToArrayAsync()
                        )
                        : ""
                );
                return new PageResult(content, q is null ? "Stikl" : $"Stikl | '{q}' results");
            }
        );
    }
}
