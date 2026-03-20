using Microsoft.AspNetCore.Antiforgery;
using Stikl.Web.DataAccess;
using Stikl.Web.Templates.Components;
using Stikl.Web.Templates.Pages;

namespace Stikl.Web.Routes;

public class PageResult(string content, string? title = null) : IResult
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
        var user = context.User;
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "text/html";
        var tokenSet = context
            .RequestServices.GetRequiredService<IAntiforgery>()
            .GetAndStoreTokens(context);
        var toasts = context
            .RequestServices.GetRequiredService<ToastHandler>()
            .ReadAndClear()
            .Select(t => t.Render());
        var pageTitle = title is null ? "Stikl" : $"Stikl | {title}";

        if (!headers.ContainsKey("HX-Request")) // this also includes boosted
            await response.WriteAsync(
                new Layout(
                    title: pageTitle,
                    content: content,
                    csrfToken: tokenSet.RequestToken!,
                    toasts: toasts,
                    auth: user.Identity?.IsAuthenticated is true
                        // TODO: cache?
                        ? new NavIdentity(
                            extraChatClasses: await context
                                .RequestServices.GetRequiredService<ChatStore>()
                                .AnyUnread(context.RequestAborted)
                                ? (string[])["pending"]
                                : []
                        )
                        : new NavLogin()
                )
            );
        else
        {
            // TODO: check if auth has changed, and if yes, also update that!
            response.Headers["HX-Retarget"] = "main";
            response.Headers["HX-Reswap"] = "innerHTML swap:300ms";
            await response.WriteAsync($"<title>{pageTitle}</title>");
            await response.WriteAsync(content + string.Join("", toasts));
        }
    }
}
