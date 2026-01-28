namespace Stikl.Web.Routes;

public class RedirectResult(string route) : IResult
{
    public async Task ExecuteAsync(HttpContext context)
    {
        var headers = context.Request.Headers;
        if (!headers.ContainsKey("HX-Request"))
            await Results.Redirect(route).ExecuteAsync(context);
        else
        {
            context.Response.Headers["Hx-Redirect"] = route;
            context.Response.StatusCode = StatusCodes.Status200OK;
        }
    }
}

public class ComponentResult(string content) : IResult
{
    public async Task ExecuteAsync(HttpContext context)
    {
        var response = context.Response;
        response.Headers.Append("Vary", "HX-Request");
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "text/html";
        await response.WriteAsync(content, context.RequestAborted);
    }
}
