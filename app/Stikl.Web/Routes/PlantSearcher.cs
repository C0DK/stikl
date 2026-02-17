using System.Runtime.CompilerServices;
using Npgsql;
using Stikl.Web.DataAccess;

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
  common_name,
  scientific_name,
  family,
  genus,
  img_regular_url,
  img_small_url
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
                    CommonName: reader.GetFieldValue<string>(0),
                    ScientificName: string.Join(" ", reader.GetFieldValue<string[]>(1)),
                    Family: reader.GetStringOrNull(2),
                    Genus: reader.GetStringOrNull(3),
                    RegularImage: reader.GetStringOrNull(4) is { Length: > 0 } url
                        ? new Uri(url)
                        : null,
                    SmallImage: reader.GetStringOrNull(5) is { Length: > 0 } url2
                        ? new Uri(url2)
                        : null
                ),
                cancellationToken
            )
        )
        {
            yield return entry;
        }
    }
}
