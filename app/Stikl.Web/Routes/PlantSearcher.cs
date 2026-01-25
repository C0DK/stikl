using System.Runtime.CompilerServices;
using Dapper;
using Npgsql;

namespace Stikl.Web.Routes;

public class PlantSearcher(NpgsqlDataSource db)
{
    // TODO paginate!
    public IAsyncEnumerable<Species> GetSearchResults(
        string query,
        CancellationToken cancellationToken
    ) => GetSearchResults(query, 30, cancellationToken);

    public async IAsyncEnumerable<Species> GetSearchResults(
        string query,
        int limit,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await using var connection = await db.OpenConnectionAsync(cancellationToken);

        foreach (
            var dbo in await connection.QueryAsync<(
                string perenual_id,
                string common_name,
                string[] scientific_name,
                string[] other_name,
                string? family,
                string? cultivar,
                string? variety,
                string? species_epithet,
                string? genus,
                string? subspecies,
                string? img_regular_url,
                string? img_small_url
            )>(
                @$"
SELECT 
  perenual_id,
  common_name,
  scientific_name,
  other_name,
  family,
  cultivar,
  variety,
  species_epithet,
  genus,
  subspecies,
  img_regular_url,
  img_small_url
FROM perenual_species
WHERE ts_rank_cd(search_vector, websearch_to_tsquery('english', @query)) > 0
ORDER BY ts_rank_cd(search_vector, websearch_to_tsquery('english', @query)) DESC
LIMIT @limit",
                new { query, limit }
            )
        )
        {
            yield return new Species(
                CommonName: dbo.common_name,
                ScientificName: string.Join(" ", dbo.scientific_name),
                Family: dbo.family,
                Genus: dbo.genus,
                RegularImage: dbo.img_regular_url is { Length: > 0 } url ? new Uri(url) : null,
                SmallImage: dbo.img_small_url is { Length: > 0 } url2 ? new Uri(url2) : null
            );
        }
    }
}
