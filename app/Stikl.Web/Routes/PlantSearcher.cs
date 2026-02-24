using System.Runtime.CompilerServices;
using Npgsql;
using Stikl.Web.DataAccess;
using Stikl.Web.Model;

namespace Stikl.Web.Routes;

public class PlantSearcher(NpgsqlDataSource db)
{
    // TODO paginate!
    public async IAsyncEnumerable<Species> GetSearchResults(
        string query,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await using var connection = await db.OpenConnectionAsync(cancellationToken);

        using var command = new NpgsqlCommand(
            @"
SELECT 
  perenual_id,
  common_name,
  scientific_name,
  family,
  genus
FROM perenual_species
WHERE ts_rank_cd(search_vector, websearch_to_tsquery('english', $1)) > 0
ORDER BY ts_rank_cd(search_vector, websearch_to_tsquery('english', $1)) DESC
LIMIT 30
",
            connection
        )
        {
            Parameters = { NpgsqlParam.Create(query) },
        };

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
        {
            yield return entry;
        }
    }
}
