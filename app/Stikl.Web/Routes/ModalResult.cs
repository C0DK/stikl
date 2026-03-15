using Stikl.Web.Templates.Components;

namespace Stikl.Web.Routes;

public class ModalResult(string title, string content) : IResult
{
    public async Task ExecuteAsync(HttpContext context)
    {
        var response = context.Response;
        var headers = context.Request.Headers;
        response.Headers.Append("Cache-Control", "no-cache");
        response.Headers.Append("HX-Reswap", "none");
        response.Headers.Append("Vary", "HX-Request, HX-Trigger-Name");
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "text/html";
        var toasts = string.Join(
            "",
            context
                .RequestServices.GetRequiredService<ToastHandler>()
                .ReadAndClear()
                .Select(t => t.Render())
        );

        await response.WriteAsync(new Modal(title: title, content: content) + toasts);
    }
}
