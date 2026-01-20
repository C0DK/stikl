using Stikl.Web.Templates.Pages;
namespace Stikl.Web.Routes;

public static class RootRouter
{
    public static void Map(WebApplication app)
    {
        app.MapGet(
            "/",
            (HttpContext context, CancellationToken cancellationToken, string? q = null) =>
            {
                var content = new IndexPage(
                      query: q,
                      searchResult: q is not null ? $"Your query was '{q}' - results are here" : ""
                    );
                if(context.IsHtmx())
                  return HtmlResult(content);
                return  HtmlResult(new Layout(
                    title: "Stikl",
                    content: content
                    ));
            }
        );
    }
    public static IResult HtmlResult(string content, int statusCode = 200) =>
        Results.Text(content, "text/html", statusCode: statusCode);
}
