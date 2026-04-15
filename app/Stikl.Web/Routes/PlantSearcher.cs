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
  p.perenual_id,
  p.common_name,
  p.scientific_name,
  p.family,
  p.genus,
  w.wikipedia_page_url,
  w.description
FROM perenual_species p
LEFT JOIN wiki_species_info w ON p.perenual_id = w.perenual_id AND w.lang = 'en'
WHERE ts_rank_cd(p.search_vector, websearch_to_tsquery('english', $1)) > 0
ORDER BY ts_rank_cd(p.search_vector, websearch_to_tsquery('english', $1)) DESC
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
                    Genus: reader.GetStringOrNull(4),
                    WikiPageUrl: reader.GetStringOrNull(5),
                    WikiDescription: reader.GetStringOrNull(6)
                ),
                cancellationToken
            )
        )
        {
            yield return entry;
        }
    }
}
