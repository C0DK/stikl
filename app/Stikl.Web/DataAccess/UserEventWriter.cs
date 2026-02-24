using Npgsql;
using NpgsqlTypes;
using Stikl.Web.Model;

namespace Stikl.Web.DataAccess;

public class UserEventWriter(NpgsqlConnection connection)
{
    private readonly UserSource readModel = new UserSource(connection);

    public async ValueTask<User> Write(
        Username username,
        UserEventPayload payload,
        CancellationToken cancellationToken
    )
    {
        await using var cmd = new NpgsqlCommand(
            @"
            INSERT INTO 
              stikl.user_event(username, version, kind, payload)
              VALUES($1, (SELECT max(version)+1 FROM stikl.user_event WHERE username = $1), $2, $3)",
            connection
        )
        {
            Parameters =
            {
                new NpgsqlParameter<string>()
                {
                    TypedValue = username,
                    NpgsqlDbType = NpgsqlDbType.Text,
                },
                new NpgsqlParameter<string>() { TypedValue = payload.EventKind },
                new NpgsqlParameter<string>()
                {
                    TypedValue = payload.Serialize(),
                    NpgsqlDbType = NpgsqlDbType.Jsonb,
                },
            },
        };
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return await readModel.Refresh(username, cancellationToken);
    }

    public async ValueTask<User> Write(
        Username username,
        int version,
        UserEventPayload payload,
        CancellationToken cancellationToken
    )
    {
        await using var cmd = new NpgsqlCommand(
            @"
            INSERT INTO 
              stikl.user_event(username, version, kind, payload)
              VALUES($1, $2, $3, $4)",
            connection
        );
        cmd.Parameters.Add(NpgsqlParam.Create(username));
        cmd.Parameters.Add(NpgsqlParam.Create(version));
        cmd.Parameters.Add(NpgsqlParam.Create(payload.EventKind));
        cmd.Parameters.Add(NpgsqlParam.Create(payload.Serialize()));
        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException exception) when (exception.ConstraintName is not null)
        {
            throw new EventBrokeConstraint(username, version);
        }
        return await readModel.Refresh(username, cancellationToken);
    }

    public class EventBrokeConstraint(string username, int version)
        : Exception($"'{username}' already had an event of version {version}") { }
}
