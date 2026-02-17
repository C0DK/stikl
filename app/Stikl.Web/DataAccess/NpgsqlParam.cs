using System.Data.Common;
using System.Runtime.CompilerServices;
using Npgsql;
using NpgsqlTypes;

namespace Stikl.Web.DataAccess;

public static class NpgsqlParam
{
    public static NpgsqlParameter<string> Create(string value) =>
        new NpgsqlParameter<string>() { TypedValue = value };

    public static NpgsqlParameter<int> Create(int value) =>
        new NpgsqlParameter<int>() { TypedValue = value };

    public static NpgsqlParameter<string> CreateJsonb(string value) =>
        new NpgsqlParameter<string>() { TypedValue = value, NpgsqlDbType = NpgsqlDbType.Jsonb };

    public static string? GetStringOrNull(this DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<string>(ordinal);

    public static async IAsyncEnumerable<T> ReadAllAsync<T>(
        this NpgsqlCommand command,
        Func<DbDataReader, T> selector,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return selector(reader);
        }
    }
}
