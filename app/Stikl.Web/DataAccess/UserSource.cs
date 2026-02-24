using System.Security.Claims;
using System.Text.Json;
using Npgsql;
using Stikl.Web.Model;
using Stikl.Web.Routes;

namespace Stikl.Web.DataAccess;

public class UserSource(NpgsqlConnection connection)
{
    public async ValueTask<User> Refresh(Username username, CancellationToken cancellationToken)
    {
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var user = await GetOrNullByEvents(username, cancellationToken);
        if (user is null)
            throw new InvalidOperationException("User doesnt exist!");

        using var command = new NpgsqlCommand(
            @"
INSERT INTO stikl.readmodel_user(
  username,
  email,
  version,
  payload
  )
VALUES(
  $1,
  $2,
  $3,
  $4 
) ON CONFLICT (username) DO UPDATE SET
  email = $2,
  version = $3,
  payload = $4
WHERE readmodel_user.version < $3
            ",
            connection,
            transaction
        )
        {
            Parameters =
            {
                NpgsqlParam.Create(username),
                NpgsqlParam.Create(user.Email),
                NpgsqlParam.Create(user.History.Last().Version),
                NpgsqlParam.CreateJsonb(JsonSerializer.Serialize(user)),
            },
        };
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return user;
    }

    public async ValueTask<User> GetFromPrincipal(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken
    ) =>
        (await GetFromPrincipalOrDefault(principal, cancellationToken))
        ?? throw new InvalidOperationException("User doesnt exist!");

    public async ValueTask<User?> GetFromPrincipalOrDefault(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken
    ) =>
        // TODO: cache?
        principal.GetEmailOrNull()
            is { } email
            ? (await GetOrNull(email, cancellationToken))
            : null;

    public async ValueTask<User?> GetOrNull(Username username, CancellationToken cancellationToken)
    {
        using var command = new NpgsqlCommand(
            "SELECT payload FROM stikl.readmodel_user WHERE username = $1",
            connection
        )
        {
            Parameters = { NpgsqlParam.Create(username) },
        };
        var dbo = await command
            .ReadAllAsync(reader => reader.GetFieldValue<string>(0), cancellationToken)
            .FirstOrDefaultAsync();
        if (dbo is null)
            return null;
        return JsonSerializer.Deserialize<User>(dbo);
    }

    public async ValueTask<User?> GetOrNull(Email email, CancellationToken cancellationToken)
    {
        using var command = new NpgsqlCommand(
            "SELECT payload FROM stikl.readmodel_user WHERE email = $1",
            connection
        )
        {
            Parameters = { NpgsqlParam.Create(email) },
        };
        var dbo = await command
            .ReadAllAsync(reader => reader.GetFieldValue<string>(0), cancellationToken)
            .FirstOrDefaultAsync();
        if (dbo is null)
            return null;
        return JsonSerializer.Deserialize<User>(dbo);
    }

    private async ValueTask<User?> GetOrNullByEvents(
        Username username,
        CancellationToken cancellationToken
    )
    {
        using var command = new NpgsqlCommand(
            @"
SELECT 
  username, 
  version,
  timestamp,
  payload
FROM stikl.user_event
WHERE username = $1
ORDER BY timestamp
",
            connection
        )
        {
            Parameters = { NpgsqlParam.Create(username) },
        };
        await using var enumerator = command
            .ReadAllAsync(
                reader => new UserEvent(
                    Username: new Username(reader.GetFieldValue<string>(0)),
                    Version: reader.GetFieldValue<int>(1),
                    Timestamp: reader.GetFieldValue<DateTimeOffset>(2),
                    Payload: UserEventPayload.Deserialize(reader.GetFieldValue<string>(3))
                ),
                cancellationToken
            )
            .GetAsyncEnumerator();

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
}
