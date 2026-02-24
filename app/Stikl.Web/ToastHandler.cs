using System.Text.Json;
using Stikl.Web.Templates.Components;
using Strongbars.Abstractions;

namespace Stikl.Web.Routes;

public class ToastHandler(IHttpContextAccessor contextAccessor)
{
    private ISession Session => contextAccessor.HttpContext!.Session;

    public record Payload(string Title, string Message)
    {
        public Template Render() => new Toast(title: Title, message: Message);
    }

    public void Add(string title, string message) => Add(new Payload(title, message));

    public void Add(Payload payload) =>
        Session.SetString(SessionKey, JsonSerializer.Serialize(Peek().Append(payload)));

    public Payload[] Peek() =>
        (
            Session.GetString(SessionKey) is { Length: > 0 } json
                ? JsonSerializer.Deserialize<Payload[]>(json)
                : null
        ) ?? [];

    public Payload[] ReadAndClear()
    {
        var toasts = Peek();
        Session.SetString(SessionKey, "");

        return toasts;
    }

    private const string SessionKey = "Toasts";
}
