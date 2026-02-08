using System.Text.Json;
using Dapper;
using Npgsql;
using Stikl.Web.Model;

namespace Stikl.Web.DataAccess;

public class UserSource(NpgsqlConnection connection)
{
    public async ValueTask<User> Refresh(Username username, CancellationToken cancellationToken)
    {
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var user = await GetOrNull(username, cancellationToken);
        if (user is null)
            throw new InvalidOperationException("User doesnt exist!");

        await connection.ExecuteAsync(
            // TODO make the conditional a bit prettier ?
            @"
INSERT INTO stikl.readmodel_user(
  username,
  email,
  version,
  payload
  )
VALUES(
  @username,
  @email,
  @version,
  CAST(@payload AS JSONB)
) ON CONFLICT (username) DO UPDATE SET
  email = CASE WHEN readmodel_user.version < @version THEN @email ELSE readmodel_user.email END,
  version = CASE WHEN readmodel_user.version < @version THEN @version ELSE readmodel_user.version END,
  payload = CASE WHEN readmodel_user.version < @version THEN CAST(@payload AS JSONB) ELSE readmodel_user.payload END
            ",
            new
            {
                username = username.Value,
                email = user.Email.Value,
                version = user.History.Last().Version,
                payload = JsonSerializer.Serialize(user),
            },
            transaction
        );
        await transaction.CommitAsync(cancellationToken);

        return user;
    }

    public async ValueTask<User?> GetOrNull(Email email, CancellationToken cancellationToken)
    {
        var dbo = await connection.QueryFirstOrDefaultAsync<string>(
            "SELECT payload FROM stikl.readmodel_user WHERE email = @email",
            new { email = email.Value }
        );

        if (dbo is null)
            return null;
        return JsonSerializer.Deserialize<User>(dbo);
    }

    private async ValueTask<User?> GetOrNull(Username username, CancellationToken cancellationToken)
    {
        await using var enumerator = GetEvents(username, cancellationToken).GetAsyncEnumerator();

        if (!await enumerator.MoveNextAsync())
            return null;

        var firstEvent = enumerator.Current;
        if (firstEvent.Payload is not UserCreated userCreated)
            throw new InvalidDataException();
        var user = userCreated.Create(firstEvent);

        while (await enumerator.MoveNextAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            user = enumerator.Current.Apply(user);
        }

        return user;
    }

    private IAsyncEnumerable<UserEvent> GetEvents(
        Username username,
        CancellationToken cancellationToken
    ) =>
        connection
            .QueryUnbufferedAsync<(
                string username,
                int version,
                DateTime timestamp,
                string kind,
                string payload
            )>(
                @"
SELECT 
  username, 
  version,
  timestamp,
  kind,
  payload
FROM stikl.user_event
WHERE username = @username
ORDER BY timestamp
",
                new { username = username.Value }
            )
            .Select(dbo => new UserEvent(
                Username: new Username(dbo.username),
                Version: dbo.version,
                Timestamp: dbo.timestamp,
                Payload: UserEventPayload.Deserialize(dbo.payload)
            ));
}
