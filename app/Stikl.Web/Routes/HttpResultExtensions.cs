using System.Net.Mime;

namespace Stikl.Web.Routes;

public static class HttpResultExtensions
{
    public static bool IsHtmx(this HttpContext context) => IsHtmx(context.Request);

    public static bool IsHtmx(this HttpRequest request) =>
        request.Headers.ContainsKey("HX-Request") && !request.Headers.ContainsKey("HX-Boosted");

    public static bool IsJson(this HttpContext context) => IsJson(context.Request);

    public static bool IsJson(this HttpRequest request) =>
        request.Headers.Accept == MediaTypeNames.Application.Json;

    public static bool IsSseRequest(this HttpContext context) => IsSseRequest(context.Request);

    public static bool IsSseRequest(this HttpRequest request) =>
        request.Headers.Accept == "text/event-stream";
}
