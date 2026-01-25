using Stikl.Web.Templates.Components;
using Stikl.Web.Templates.Pages;

namespace Stikl.Web.Routes;

public static class RootRouter
{
    public static void Map(WebApplication app)
    {
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
                    searchBlock : new Search(query: q),
                    searchResult: !string.IsNullOrWhiteSpace(q)
                        ? new SearchResults(
                            results: await searcher
                                .GetSearchResults(q, cancellationToken)
                                .Select(Species.ToPlantCard)
                                // TODO: strongbars array should not require ToString here..!
                                .Select(s => s.ToString())
                                .ToArrayAsync()
                        )
                        : ""
                );
                if (context.IsHtmx())
                    return HtmlResult(content);
                return HtmlResult(new Layout(title: "Stikl", content: content));
            }
        );
    }

    public static IResult HtmlResult(string content, int statusCode = 200) =>
        Results.Text(content, "text/html", statusCode: statusCode);
}
