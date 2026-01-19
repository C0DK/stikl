namespace Stikl.Web.Routes;

public static class RootRouter
{
    public static void Map(WebApplication app)
    {
        app.MapGet(
            "/",
            (HttpContext context, CancellationToken cancellationToken) =>
            {
                return "Hello";
            }
        );
    }
}
