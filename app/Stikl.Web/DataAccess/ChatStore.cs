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
stikl.chat_event(sender, recipient, kind, payload)
VALUES($1, $2, $3, $4)
RETURNING pk, sender, recipient, timestamp, payload;
",
            connection,
            transaction
        )
        {
            Parameters =
            {
                NpgsqlParam.Create(sender),
                NpgsqlParam.Create(recipient),
                NpgsqlParam.Create(Message.Kind),
                NpgsqlParam.Create(new Message(content).Serialize()),
            },
        };
        var message = await command.FirstAsync(
            reader =>
                (ChatEvent)
                    new ChatEvent(
                        Pk: reader.GetFieldValue<int>(0),
                        Sender: new Username(reader.GetFieldValue<string>(1)),
                        Recipient: new Username(reader.GetFieldValue<string>(2)),
                        Timestamp: reader.GetFieldValue<DateTimeOffset>(3),
                        Payload: ChatEventPayload.Deserialize(reader.GetFieldValue<string>(4))
                    ),
            cancellationToken
        );
        await new NpgsqlCommand("SELECT pg_notify('chat_messages', $1);", connection, transaction)
        {
            Parameters = { NpgsqlParam.Create(JsonSerializer.Serialize(message)) },
        }.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync();
    }

    public async ValueTask<bool> AnyUnread(CancellationToken cancellationToken)
    {
        var requestee = httpContext.User.GetUsername();
        // Doesnt seem to work if you sent?
        using var command = new NpgsqlCommand(
            @"
SELECT
  true
FROM stikl.chat_event
WHERE recipient =$1
  AND timestamp > (SELECT MAX(timestamp) FROM stikl.chat_event WHERE sender = $1 AND kind = 'read')
GROUP BY (CASE WHEN sender = $1 THEN recipient ELSE sender END)
LIMIT 1
",
            connection
        )
        {
            Parameters = { NpgsqlParam.Create(requestee) },
        };
        return await command.FirstOrDefaultAsync<bool>(
            reader => reader.GetFieldValue<bool>(0),
            cancellationToken
        );
    }

    public async ValueTask<Username?> LatestChat(CancellationToken cancellationToken)
    {
        var requestee = httpContext.User.GetUsername();
        using var command = new NpgsqlCommand(
            @"
SELECT
(CASE WHEN sender = $1 THEN recipient ELSE sender END)
FROM stikl.chat_event
WHERE sender = $1 OR recipient =$1
ORDER BY timestamp DESC
LIMIT 1
",
            connection
        )
        {
            Parameters = { NpgsqlParam.Create(requestee) },
        };
        return await command.FirstOrDefaultAsync<Username?>(
            reader => reader.IsDBNull(0) ? null : new Username(reader.GetFieldValue<string>(0)),
            cancellationToken
        );
    }

    public async IAsyncEnumerable<(
        Username Username,
        ChatEvent Message,
        bool Unread
    )> ListConversations([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // todo: cursor
        var requestee = httpContext.User.GetUsername();
        using var command = new NpgsqlCommand(
            @"
SELECT 
  pk, 
  sender,
  recipient,
  timestamp,
  payload,
  (recipient = $1 AND timestamp > (SELECT MAX(timestamp) FROM stikl.chat_event WHERE sender = $1 AND kind = 'read')) as unread
FROM stikl.chat_event
WHERE pk IN (
  SELECT
   MAX(pk)
 FROM stikl.chat_event
 WHERE (sender = $1 OR recipient = $1) AND kind != 'read'
 GROUP BY (CASE WHEN sender = $1 THEN recipient ELSE sender END)
)
ORDER BY timestamp
",
            connection
        )
        {
            Parameters = { NpgsqlParam.Create(requestee) },
        };
        await foreach (
            var (@event, unread) in command.ReadAllAsync(
                reader =>
                    (
                        chatEvent: new ChatEvent(
                            Pk: reader.GetFieldValue<int>(0),
                            Sender: new Username(reader.GetFieldValue<string>(1)),
                            Recipient: new Username(reader.GetFieldValue<string>(2)),
                            Timestamp: reader.GetFieldValue<DateTimeOffset>(3),
                            Payload: ChatEventPayload.Deserialize(reader.GetFieldValue<string>(4))
                        ),
                        unread: reader.GetFieldValue<bool>(5)
                    ),
                cancellationToken
            )
        )
            // TODO: join user to get first name)
            yield return (
                @event.Sender == requestee ? @event.Recipient : @event.Sender,
                @event,
                unread
            );
    }

    public async ValueTask UpdateRead(Username other, CancellationToken cancellationToken)
    {
        var requestee = httpContext.User.GetUsername();
        using var command = new NpgsqlCommand(
            @"
INSERT INTO 
  stikl.chat_event(sender, recipient, kind, payload)
SELECT $1, $2, $3, $4
WHERE (
  (SELECT max(timestamp) 
   FROM stikl.chat_event
   WHERE sender = $2 AND recipient = $1 AND kind != 'read')
  >
  COALESCE(
    (
       SELECT max(timestamp) 
       FROM stikl.chat_event
       WHERE sender = $1 AND recipient = $2 AND kind = 'read'
    ),
    '-infinity'
  )
)
RETURNING pk, sender, recipient, timestamp, payload;
",
            connection
        )
        {
            Parameters =
            {
                NpgsqlParam.Create(requestee),
                NpgsqlParam.Create(other),
                NpgsqlParam.Create(Read.Kind),
                NpgsqlParam.CreateJsonb(new Read().Serialize()),
            },
        };

        var update = await command.FirstOrDefaultAsync(
            reader =>
                (ChatEvent)
                    new ChatEvent(
                        Pk: reader.GetFieldValue<int>(0),
                        Sender: new Username(reader.GetFieldValue<string>(1)),
                        Recipient: new Username(reader.GetFieldValue<string>(2)),
                        Timestamp: reader.GetFieldValue<DateTimeOffset>(3),
                        Payload: ChatEventPayload.Deserialize(reader.GetFieldValue<string>(4))
                    ),
            cancellationToken
        );
        if (update is not null)
            await new NpgsqlCommand("SELECT pg_notify('chat_messages', $1);", connection)
            {
                Parameters = { NpgsqlParam.Create(JsonSerializer.Serialize(update)) },
            }.ExecuteNonQueryAsync(cancellationToken);
    }

    public async IAsyncEnumerable<ChatEvent> ReadAll(
        Username other,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var requestee = httpContext.User.GetUsername();
        await UpdateRead(other, cancellationToken);
        // Should we dispose here?
        using var command = new NpgsqlCommand(
            @"
SELECT 
  pk, sender, recipient, timestamp, payload
FROM stikl.chat_event
WHERE (
  (sender = $1 AND recipient = $2) OR (sender = $2 AND recipient = $1))
  AND (kind != 'read' OR pk = (SELECT MAX(pk) FROM stikl.chat_event AS reads WHERE kind='read' AND reads.sender = $2))
ORDER BY timestamp;
",
            connection
        )
        {
            Parameters = { NpgsqlParam.Create(requestee), NpgsqlParam.Create(other) },
        };
        await foreach (
            var message in command.ReadAllAsync(
                reader => new ChatEvent(
                    Pk: reader.GetFieldValue<int>(0),
                    Sender: new Username(reader.GetFieldValue<string>(1)),
                    Recipient: new Username(reader.GetFieldValue<string>(2)),
                    Timestamp: reader.GetFieldValue<DateTimeOffset>(3),
                    Payload: ChatEventPayload.Deserialize(reader.GetFieldValue<string>(4))
                ),
                cancellationToken
            )
        )
            yield return message;
    }
    // TODO: do a sorta broker that will handle the notify and listen..
}
