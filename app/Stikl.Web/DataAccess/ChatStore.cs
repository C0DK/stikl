using System.Runtime.CompilerServices;
using System.Text.Json;
using Npgsql;
using Stikl.Web.Model;
using Stikl.Web.Routes;

namespace Stikl.Web.DataAccess;

public class ChatStore(NpgsqlConnection connection, HttpContext httpContext)
{
    public async ValueTask SendMessage(
        Username recipient,
        string content,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var sender = httpContext.User.GetUsername();
        // TODO: separate method to "create" vs write event. here we should check if id exists.
        await using var command = new NpgsqlCommand(
            @"
INSERT INTO 
stikl.chat_message(sender, recipient, message)
VALUES($1, $2, $3)
RETURNING pk, sender, recipient, timestamp, message;
",
            connection,
            transaction
        )
        {
            Parameters =
            {
                NpgsqlParam.Create(sender),
                NpgsqlParam.Create(recipient),
                NpgsqlParam.Create(content),
            },
        };
        var message = await command
            .ReadAllAsync(
                reader => new ChatMessage(
                    Pk: reader.GetFieldValue<int>(0),
                    Sender: new Username(reader.GetFieldValue<string>(1)),
                    Recipient: new Username(reader.GetFieldValue<string>(2)),
                    Timestamp: reader.GetFieldValue<DateTimeOffset>(3),
                    Message: reader.GetFieldValue<string>(4)
                ),
                cancellationToken
            )
            .FirstAsync();
        await new NpgsqlCommand("SELECT pg_notify('chat_messages', $1);", connection, transaction)
        {
            Parameters = { NpgsqlParam.Create(JsonSerializer.Serialize(message)) },
        }.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync();
    }

    public async IAsyncEnumerable<ChatMessage> ReadAll(
        Username other,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var requestee = httpContext.User.GetUsername();
        using var command = new NpgsqlCommand(
            @"
SELECT 
  pk, sender, recipient, timestamp, message
FROM stikl.chat_message
WHERE (sender = $1 AND recipient = $2) OR (sender = $2 AND recipient = $1)
ORDER BY timestamp
",
            connection
        )
        {
            Parameters = { NpgsqlParam.Create(requestee), NpgsqlParam.Create(other) },
        };
        await foreach (
            var message in command.ReadAllAsync(
                reader => new ChatMessage(
                    Pk: reader.GetFieldValue<int>(0),
                    Sender: new Username(reader.GetFieldValue<string>(1)),
                    Recipient: new Username(reader.GetFieldValue<string>(2)),
                    Timestamp: reader.GetFieldValue<DateTimeOffset>(3),
                    Message: reader.GetFieldValue<string>(4)
                ),
                cancellationToken
            )
        )
            yield return message;
    }
    // TODO: do a sorta broker that will handle the notify and listen..
}
