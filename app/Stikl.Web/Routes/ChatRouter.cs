using System.Web;
using Npgsql;
using Stikl.Web.DataAccess;
using Stikl.Web.Model;
using Stikl.Web.Templates.Pages;

namespace Stikl.Web.Routes;

public static class ChatRouter
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // TODO: SSE
        app.MapGet(
                "/{username}",
                async (
                    HttpContext context,
                    PlantSearcher searcher,
                    NpgsqlDataSource db, // TODO: can we get the connection parts somewhat better from like an transient requirement?
                    ChatBroker broker,
                    Username username,
                    ILogger logger,
                    CancellationToken cancellationToken
                ) =>
                {
                    await using var connection = await db.OpenConnectionAsync(cancellationToken);
                    var users = new UserSource(connection);
                    var other = await users.GetOrNull(username, cancellationToken);
                    if (other is null)
                        return Results.NotFound();
                    if (context.Request.Headers.Accept == "text/event-stream")
                        return new ChatServerSentEventResult(other, broker);

                    var chat = new ChatStore(connection, context);

                    return new PageResult(
                        new ChatPage(
                            username: other.UserName,
                            chatForm: new Templates.Components.ChatForm(
                                username: other.UserName,
                                firstName: other.FirstName
                            ),
                            firstName: other.FirstName,
                            lastName: other.LastName,
                            messages: await chat.ReadAll(other.UserName, cancellationToken)
                                .Select(message => RenderChatMessage(message, other))
                                .ToArrayAsync()
                        )
                    );
                }
            )
            .RequireAuthorization(); // require signup to be done!
        app.MapPost(
                "/{username}",
                async (
                    HttpContext context,
                    PlantSearcher searcher,
                    NpgsqlDataSource db, // TODO: can we get the connection parts somewhat better from like an transient requirement?
                    Username username,
                    CancellationToken cancellationToken
                ) =>
                {
                    var form = context.Request.Form;
                    var message = form.GetString("message")?.Trim();
                    if (string.IsNullOrEmpty(message))
                        return Results.BadRequest("Chat message where you at?");
                    await using var connection = await db.OpenConnectionAsync(cancellationToken);
                    var users = new UserSource(connection);
                    var other = await users.GetOrNull(username, cancellationToken);
                    if (other is null)
                        return Results.NotFound();
                    var chat = new ChatStore(connection, context);
                    await chat.SendMessage(other.UserName, message, cancellationToken);

                    return new PartialResult(
                        new Templates.Components.ChatForm(
                            username: other.UserName,
                            firstName: other.FirstName
                        )
                    );
                }
            )
            .RequireAuthorization(); // require signup to be done!
    }

    private static string RenderChatMessage(ChatMessage message, User other) =>
        new Templates.Components.ChatMessage(
            author: message.Sender == other.UserName ? other.FirstName : "you",
            message: HttpUtility.HtmlEncode(message.Message),
            timestamp: message.Timestamp.ToString(
                "HH:mm" // TODO: better timestamp.
            ),
            extraClasses: message.Sender == other.UserName ? "ours" : ""
        );

    public class ChatServerSentEventResult(User other, ChatBroker broker) : ServerSentEventResult
    {
        public override async IAsyncEnumerator<string> GetUpdates(
            CancellationToken cancellationToken
        )
        {
            await using var enumerator = broker.Subscribe(other.UserName, cancellationToken);
            while (await enumerator.MoveNextAsync())
                yield return @$"
<div hx-swap-oob=""beforeend:#chat"">
  {RenderChatMessage(enumerator.Current, other)}
</div>
";
        }
    }
}
