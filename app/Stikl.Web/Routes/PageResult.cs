using Stikl.Web.Templates.Pages;

namespace Stikl.Web.Routes;

public class PageResult(string content, string title = "Stikl") : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;
        var headers = httpContext.Request.Headers;
        // caching and htmx is dumb
        if (headers.ContainsKey("HX-Request"))
        {
            response.Headers.Append("Cache-Control", "no-cache");
        }


        response.Headers.Append("Vary", "HX-Request, HX-Trigger-Name");
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "text/html";

        if (!headers.ContainsKey("HX-Request"))  // this also includes boosted
            await response.WriteAsync(new Layout(title: title, content: content));
        else
        // TODO: update title 
            await response.WriteAsync(content);
    }
}

