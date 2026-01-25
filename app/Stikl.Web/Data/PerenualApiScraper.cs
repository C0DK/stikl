using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Npgsql;
using NpgsqlTypes;

namespace Stikl.Web.Data;

public class PerenualApiScraper(
    NpgsqlConnection connection,
    HttpClient http,
    ILogger logger,
    string apiKey
)
{
    private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
    {
        //RespectRequiredConstructorParameters = true,
        //RespectNullableAnnotations = true,
    };

    public ValueTask Scrape(CancellationToken cancellationToken = default) =>
        Scrape(1, cancellationToken);

    public async ValueTask Scrape(int startPage, CancellationToken cancellationToken = default)
    {
        logger.ForContext("startPage", startPage).Debug("Start scraping");
        await Write(Load(startPage, cancellationToken), cancellationToken);
    }

    private async IAsyncEnumerable<SpeciesDto> Load(
        int startPage,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        int page = startPage;

        while (page < 100)
        {
            var result = await LoadPage(page, cancellationToken);
            foreach (var entry in result.Data)
                yield return entry;

            page++;
        }
    }

    private async ValueTask<PageDto> LoadPage(int page, CancellationToken cancellationToken)
    {
        var url = $"https://perenual.com/api/v2/species-list?page={page}&key={apiKey}";
        var response = await http.GetAsync(url, cancellationToken);
        logger.ForContext("page", page).Debug("Loading page");

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            logger.ForContext("page", page).Warning("Too many requests. backing off");
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

            response = await http.GetAsync(url, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
            throw new InvalidDataException($"API failed with {response.StatusCode}");
        logger.ForContext("page", page).Debug("Loaded page");

        return await response.Content.ReadFromJsonAsync<PageDto>(
                cancellationToken: cancellationToken,
                options: serializerOptions
            ) ?? throw new NullReferenceException("Could not deserialize?");
    }

    private async ValueTask Write(
        IAsyncEnumerable<SpeciesDto> entries,
        CancellationToken cancellationToken
    )
    {
        var i = 0;
        await foreach (var batch in entries.Batch(100, cancellationToken))
        {
            logger.ForContext("batch", i++).Debug("Writing to db");
            // better batch size when we know it works.
            // TODO: handle on conflict

            using (
                var writer = connection.BeginBinaryImport(
                    @"
COPY perenual_species (
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
  ) FROM STDIN (FORMAT BINARY)
"
                )
            )
            {
                foreach (var entry in batch)
                {
                    await writer.StartRowAsync();
                    await writer.WriteAsync(entry.Id, NpgsqlDbType.Integer, cancellationToken);
                    await writer.WriteAsync(entry.CommonName, NpgsqlDbType.Text, cancellationToken);
                    await writer.WriteAsync(
                        entry.ScientificName,
                        NpgsqlDbType.Array | NpgsqlDbType.Text,
                        cancellationToken
                    );
                    await writer.WriteAsync(
                        entry.OtherName,
                        NpgsqlDbType.Array | NpgsqlDbType.Text,
                        cancellationToken
                    );

                    await writer.WriteAsync(entry.Family, NpgsqlDbType.Text, cancellationToken);
                    await writer.WriteAsync(entry.Cultivar, NpgsqlDbType.Text, cancellationToken);
                    await writer.WriteAsync(entry.Variety, NpgsqlDbType.Text, cancellationToken);
                    await writer.WriteAsync(
                        entry.SpeciesEpithet,
                        NpgsqlDbType.Text,
                        cancellationToken
                    );
                    await writer.WriteAsync(entry.Genus, NpgsqlDbType.Text, cancellationToken);
                    await writer.WriteAsync(entry.Subspecies, NpgsqlDbType.Text, cancellationToken);
                    if (entry.DefaultImage?.RegularUrl is not null)
                        await writer.WriteAsync(
                            entry.DefaultImage.RegularUrl,
                            NpgsqlDbType.Text,
                            cancellationToken
                        );
                    else
                        await writer.WriteNullAsync(cancellationToken);
                    if (entry.DefaultImage?.SmallUrl is not null)
                        await writer.WriteAsync(
                            entry.DefaultImage.SmallUrl,
                            NpgsqlDbType.Text,
                            cancellationToken
                        );
                    else
                        await writer.WriteNullAsync(cancellationToken);
                }

                writer.Complete();
            }
        }
    }

    // TODO: get all the additional info too?

    public record PageDto(
        [property: JsonPropertyName("current_page")] int currentPage,
        [property: JsonPropertyName("last_page")] int lastPage,
        [property: JsonPropertyName("data")] SpeciesDto[] Data
    );

    public record SpeciesDto(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("common_name")] string CommonName,
        [property: JsonPropertyName("scientific_name")] List<string> ScientificName,
        [property: JsonPropertyName("other_name")] List<string> OtherName,
        [property: JsonPropertyName("family")] string? Family,
        [property: JsonPropertyName("hybrid")] string? Hybrid,
        [property: JsonPropertyName("authority")] string? Authority,
        [property: JsonPropertyName("subspecies")] string? Subspecies,
        [property: JsonPropertyName("cultivar")] string? Cultivar,
        [property: JsonPropertyName("variety")] string? Variety,
        [property: JsonPropertyName("species_epithet")] string? SpeciesEpithet,
        [property: JsonPropertyName("genus")] string? Genus,
        [property: JsonPropertyName("default_image")] DefaultImage? DefaultImage
    );

    public record DefaultImage(
        [property: JsonPropertyName("license")] int? License,
        [property: JsonPropertyName("license_name")] string? LicenseName,
        [property: JsonPropertyName("license_url")] string? LicenseUrl,
        [property: JsonPropertyName("original_url")] string? OriginalUrl,
        [property: JsonPropertyName("regular_url")] string? RegularUrl,
        [property: JsonPropertyName("medium_url")] string? MediumUrl,
        [property: JsonPropertyName("small_url")] string? SmallUrl,
        [property: JsonPropertyName("thumbnail")] string? Thumbnail
    );
}
