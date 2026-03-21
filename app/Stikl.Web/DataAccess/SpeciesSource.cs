using System.Runtime.CompilerServices;
using Npgsql;
using Stikl.Web.Model;

namespace Stikl.Web.DataAccess;

public class SpeciesSource(NpgsqlConnection connection)
{
    public async IAsyncEnumerable<Species> Get(
        IEnumerable<SpeciesId> ids,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        using var command = new NpgsqlCommand(
            @"
SELECT 
  perenual_id,
  common_name,
  scientific_name,
  family,
  genus
FROM perenual_species
WHERE perenual_id = ANY($1)
",
            connection
        );
        command.Parameters.Add(
            new NpgsqlParameter<int[]> { TypedValue = ids.Select(i => i.Value).ToArray() }
        );

        await foreach (
            var entry in command.ReadAllAsync(
                reader => new Species(
                    Id: new SpeciesId(reader.GetFieldValue<int>(0)),
                    CommonName: reader.GetFieldValue<string>(1),
                    ScientificName: string.Join(" ", reader.GetFieldValue<string[]>(2)),
                    Family: reader.GetStringOrNull(3),
                    Genus: reader.GetStringOrNull(4)
                ),
                cancellationToken
            )
        )
            yield return entry;
    }

    public async ValueTask<Species?> Get(SpeciesId id, CancellationToken cancellationToken)
    {
        using var command = new NpgsqlCommand(
            @"
SELECT 
  perenual_id,
  common_name,
  scientific_name,
  family,
  genus
FROM perenual_species
WHERE perenual_id = $1
",
            connection
        );

        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = id });

        return await command
            .ReadAllAsync(
                reader => new Species(
                    Id: new SpeciesId(reader.GetFieldValue<int>(0)),
                    CommonName: reader.GetFieldValue<string>(1),
                    ScientificName: string.Join(" ", reader.GetFieldValue<string[]>(2)),
                    Family: reader.GetStringOrNull(3),
                    Genus: reader.GetStringOrNull(4)
                ),
                cancellationToken
            )
            .SingleOrDefaultAsync();
    }
}
