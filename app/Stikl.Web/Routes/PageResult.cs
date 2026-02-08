using Microsoft.AspNetCore.Antiforgery;
using Stikl.Web.Templates.Components;
using Stikl.Web.Templates.Pages;

namespace Stikl.Web.Routes;

public class PageResult(string content, string title = "Stikl") : IResult
{
    public async Task ExecuteAsync(HttpContext context)
    {
        var response = context.Response;
        var headers = context.Request.Headers;
        // caching and htmx is dumb
        if (headers.ContainsKey("HX-Request"))
        {
            response.Headers.Append("Cache-Control", "no-cache");
        }

        response.Headers.Append("Vary", "HX-Request, HX-Trigger-Name");
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "text/html";
        var user = context.User;
        var tokenSet = context
            .RequestServices.GetRequiredService<IAntiforgery>()
            .GetAndStoreTokens(context);

        if (!headers.ContainsKey("HX-Request")) // this also includes boosted
            await response.WriteAsync(
                new Layout(
                    title: title,
                    content: content,
                    csrfToken: tokenSet.RequestToken!,
                    auth: user.Identity?.IsAuthenticated is true
                        // email shouldn't be null but fallback is nice.
                        ? new NavIdentity(
                            user.GetFirstNameOrNull() ?? user.GetEmailOrNull() ?? "you"
                        )
                        : new NavLogin()
                )
            );
        else
        {
            response.Headers["HX-Retarget"] = "main";
            await response.WriteAsync($"<title>{title}</title>");
            await response.WriteAsync(content);
        }
    }
}
