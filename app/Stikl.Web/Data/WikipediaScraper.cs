using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Npgsql;
using NpgsqlTypes;

namespace Stikl.Web.Data;

/// <summary>
/// Scrapes Wikipedia/Wikidata for each entry in perenual_species and stores the results
/// in wiki_species_info. Fetches description, edibility, hardiness zones, conservation
/// status, and parent taxon (for species-tree relationships).
///
/// Run via: WIKI_SCRAPE=true dotnet run
/// </summary>
public partial class WikipediaScraper(NpgsqlConnection connection, HttpClient http, ILogger logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // IUCN conservation status Wikidata IDs → human-readable labels
    private static readonly Dictionary<string, string> ConservationStatusLabels = new()
    {
        ["Q211005"] = "Least Concern",
        ["Q719675"] = "Near Threatened",
        ["Q278113"] = "Vulnerable",
        ["Q11394"] = "Endangered",
        ["Q219127"] = "Critically Endangered",
        ["Q239509"] = "Extinct in the Wild",
        ["Q237350"] = "Extinct",
        ["Q3245245"] = "Data Deficient",
        ["Q2708480"] = "Not Evaluated",
    };

    // Wikidata Q-IDs that indicate a plant is edible
    private static readonly HashSet<string> EdibleQIds =
    [
        "Q145409", // edible plant
        "Q11004", // vegetable
        "Q3314483", // fruit
        "Q1364", // herb
        "Q2820052", // food plant
        "Q207123", // edible fruit
        "Q728937", // culinary herb
        "Q80235", // fruit vegetable
        "Q1055684", // edible mushroom (in case of fungi)
    ];

    public async ValueTask Scrape(CancellationToken cancellationToken = default)
    {
        logger.Information("Starting Wikipedia plant scraper");
        var species = await GetUnscrapedSpecies(cancellationToken);
        logger
            .ForContext("count", species.Count)
            .Information("Found species to scrape from Wikipedia");

        foreach (var (perenualId, scientificName) in species)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            await ScrapeOne(perenualId, scientificName, cancellationToken);
            // Be a polite citizen: ~2 req/s to Wikipedia
            await Task.Delay(500, cancellationToken);
        }

        logger.Information("Wikipedia scraping completed");
    }

    private async Task<List<(int Id, string ScientificName)>> GetUnscrapedSpecies(
        CancellationToken ct
    )
    {
        await using var cmd = new NpgsqlCommand(
            """
            SELECT p.perenual_id, p.scientific_name[1]
            FROM perenual_species p
            LEFT JOIN wiki_species_info w ON p.perenual_id = w.perenual_id
            WHERE w.perenual_id IS NULL
              AND p.scientific_name[1] IS NOT NULL
            ORDER BY p.perenual_id
            """,
            connection
        );

        var results = new List<(int, string)>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            results.Add((reader.GetInt32(0), reader.GetString(1)));
        return results;
    }

    private async ValueTask ScrapeOne(int perenualId, string scientificName, CancellationToken ct)
    {
        logger
            .ForContext("perenualId", perenualId)
            .ForContext("scientificName", scientificName)
            .Debug("Scraping Wikipedia");
        try
        {
            var title = await SearchWikipedia(scientificName, ct);
            if (title is null)
            {
                await InsertEmpty(perenualId, ct);
                return;
            }

            var summary = await GetSummary(title, ct);
            if (summary is null)
            {
                await InsertEmpty(perenualId, ct);
                return;
            }

            WikidataResult? wikidata = null;
            if (summary.WikidataItem is not null)
                wikidata = await GetWikidataResult(summary.WikidataItem, ct);

            var hardinessZones = ExtractHardinessZones(summary.Extract);
            await Insert(perenualId, summary, wikidata, hardinessZones, ct);

            logger
                .ForContext("perenualId", perenualId)
                .ForContext("wikidataId", summary.WikidataItem)
                .Debug("Scraped Wikipedia successfully");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger
                .ForContext("perenualId", perenualId)
                .Warning(ex, "Wikipedia scrape failed, inserting empty record");
            await InsertEmpty(perenualId, ct);
        }
    }

    // Search Wikipedia by scientific name; returns page title of top result
    private async Task<string?> SearchWikipedia(string query, CancellationToken ct)
    {
        var encoded = Uri.EscapeDataString(query);
        var url =
            $"https://en.wikipedia.org/w/api.php?action=query&list=search&srsearch={encoded}&srlimit=1&srnamespace=0&format=json&utf8=";
        var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
            return null;
        var doc = await resp.Content.ReadFromJsonAsync<WikiSearchResponse>(JsonOptions, ct);
        return doc?.Query?.Search?.FirstOrDefault()?.Title;
    }

    // Wikipedia REST summary API returns description, extract, and wikidata_item
    private async Task<SummaryResponse?> GetSummary(string title, CancellationToken ct)
    {
        var encoded = Uri.EscapeDataString(title.Replace(" ", "_"));
        var resp = await http.GetAsync(
            $"https://en.wikipedia.org/api/rest_v1/page/summary/{encoded}",
            ct
        );
        if (!resp.IsSuccessStatusCode)
            return null;
        return await resp.Content.ReadFromJsonAsync<SummaryResponse>(JsonOptions, ct);
    }

    private async Task<WikidataResult?> GetWikidataResult(string qid, CancellationToken ct)
    {
        var url =
            $"https://www.wikidata.org/w/api.php?action=wbgetentities&ids={qid}&props=claims&format=json";
        var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
            return null;

        var doc = await resp.Content.ReadFromJsonAsync<WdEntitiesResponse>(JsonOptions, ct);
        if (doc?.Entities is null || !doc.Entities.TryGetValue(qid, out var entity))
            return null;

        var result = new WikidataResult();

        if (entity.Claims is null)
            return result;

        // P171: parent taxon — gives one level up in the taxonomic tree
        if (entity.Claims.TryGetValue("P171", out var parentClaims))
        {
            var parentQid = parentClaims.FirstOrDefault()?.Mainsnak?.Datavalue?.Value?.Id;
            if (parentQid is not null)
            {
                result.ParentTaxonWikidataId = parentQid;
                await Task.Delay(200, ct);
                result.ParentTaxonName = await GetEntityLabel(parentQid, ct);
            }
        }

        // P141: IUCN conservation status
        if (entity.Claims.TryGetValue("P141", out var consClaims))
        {
            var consQid = consClaims.FirstOrDefault()?.Mainsnak?.Datavalue?.Value?.Id;
            if (consQid is not null && ConservationStatusLabels.TryGetValue(consQid, out var label))
                result.ConservationStatus = label;
        }

        // P279 (subclass of) and P31 (instance of): check for edibility
        var typeIds = entity
            .Claims.Where(c => c.Key is "P279" or "P31")
            .SelectMany(c => c.Value)
            .Select(c => c.Mainsnak?.Datavalue?.Value?.Id)
            .OfType<string>()
            .ToHashSet();

        if (typeIds.Overlaps(EdibleQIds))
            result.Edible = true;

        return result;
    }

    // Fetch the English label for a Wikidata entity (used to resolve parent taxon name)
    private async Task<string?> GetEntityLabel(string qid, CancellationToken ct)
    {
        var url =
            $"https://www.wikidata.org/w/api.php?action=wbgetentities&ids={qid}&props=labels&format=json&languages=en";
        var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
            return null;
        var doc = await resp.Content.ReadFromJsonAsync<WdEntitiesResponse>(JsonOptions, ct);
        if (doc?.Entities is null || !doc.Entities.TryGetValue(qid, out var entity))
            return null;
        return entity.Labels?.TryGetValue("en", out var label) == true ? label.Value : null;
    }

    // Extracts USDA hardiness zone ranges from Wikipedia article text
    [GeneratedRegex(
        @"(?:USDA\s+)?(?:hardiness\s+)?zones?\s+(\d+(?:[ab])?(?:\s*[-–]\s*\d+(?:[ab])?)?)",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex HardinessZoneRegex();

    private static string? ExtractHardinessZones(string? extract)
    {
        if (string.IsNullOrWhiteSpace(extract))
            return null;
        var match = HardinessZoneRegex().Match(extract);
        return match.Success ? match.Value : null;
    }

    private async ValueTask Insert(
        int perenualId,
        SummaryResponse summary,
        WikidataResult? wikidata,
        string? hardinessZones,
        CancellationToken ct
    )
    {
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO wiki_species_info (
                perenual_id, wikipedia_title, wikipedia_page_id, wikidata_id,
                description, extract, edible, hardiness_zones,
                conservation_status, parent_taxon_name, parent_taxon_wikidata_id
            ) VALUES (
                @perenualId, @title, @pageId, @wikidataId,
                @description, @extract, @edible, @hardinessZones,
                @conservationStatus, @parentTaxonName, @parentTaxonWikidataId
            ) ON CONFLICT (perenual_id) DO UPDATE SET
                wikipedia_title          = EXCLUDED.wikipedia_title,
                wikipedia_page_id        = EXCLUDED.wikipedia_page_id,
                wikidata_id              = EXCLUDED.wikidata_id,
                description              = EXCLUDED.description,
                extract                  = EXCLUDED.extract,
                edible                   = EXCLUDED.edible,
                hardiness_zones          = EXCLUDED.hardiness_zones,
                conservation_status      = EXCLUDED.conservation_status,
                parent_taxon_name        = EXCLUDED.parent_taxon_name,
                parent_taxon_wikidata_id = EXCLUDED.parent_taxon_wikidata_id,
                scraped_at               = now()
            """,
            connection
        );

        cmd.Parameters.AddWithValue("perenualId", perenualId);
        AddText(cmd, "title", summary.Title);
        AddInt(cmd, "pageId", summary.PageId);
        AddText(cmd, "wikidataId", summary.WikidataItem);
        AddText(cmd, "description", summary.Description);
        AddText(cmd, "extract", summary.Extract);
        AddBool(cmd, "edible", wikidata?.Edible);
        AddText(cmd, "hardinessZones", hardinessZones);
        AddText(cmd, "conservationStatus", wikidata?.ConservationStatus);
        AddText(cmd, "parentTaxonName", wikidata?.ParentTaxonName);
        AddText(cmd, "parentTaxonWikidataId", wikidata?.ParentTaxonWikidataId);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async ValueTask InsertEmpty(int perenualId, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO wiki_species_info (perenual_id) VALUES (@id) ON CONFLICT DO NOTHING",
            connection
        );
        cmd.Parameters.AddWithValue("id", perenualId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static void AddText(NpgsqlCommand cmd, string name, string? value)
    {
        cmd.Parameters.Add(name, NpgsqlDbType.Text).Value = (object?)value ?? DBNull.Value;
    }

    private static void AddInt(NpgsqlCommand cmd, string name, int? value)
    {
        cmd.Parameters.Add(name, NpgsqlDbType.Integer).Value = (object?)value ?? DBNull.Value;
    }

    private static void AddBool(NpgsqlCommand cmd, string name, bool? value)
    {
        cmd.Parameters.Add(name, NpgsqlDbType.Boolean).Value = (object?)value ?? DBNull.Value;
    }

    // --- DTOs ---

    private record WikiSearchResponse([property: JsonPropertyName("query")] WikiQuery? Query);

    private record WikiQuery([property: JsonPropertyName("search")] WikiSearchItem[]? Search);

    private record WikiSearchItem(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("pageid")] int PageId
    );

    private record SummaryResponse(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("pageid")] int? PageId,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("extract")] string? Extract,
        [property: JsonPropertyName("wikidata_item")] string? WikidataItem
    );

    private record WdEntitiesResponse(
        [property: JsonPropertyName("entities")] Dictionary<string, WdEntity>? Entities
    );

    private record WdEntity(
        [property: JsonPropertyName("claims")] Dictionary<string, WdClaim[]>? Claims,
        [property: JsonPropertyName("labels")] Dictionary<string, WdLabel>? Labels
    );

    private record WdClaim([property: JsonPropertyName("mainsnak")] WdMainsnak? Mainsnak);

    private record WdMainsnak([property: JsonPropertyName("datavalue")] WdDatavalue? Datavalue);

    private record WdDatavalue([property: JsonPropertyName("value")] WdValue? Value);

    // Wikidata entity-type values use an "id" field (e.g. "Q12345")
    private record WdValue([property: JsonPropertyName("id")] string? Id);

    private record WdLabel([property: JsonPropertyName("value")] string? Value);

    private sealed class WikidataResult
    {
        public string? ParentTaxonWikidataId { get; set; }
        public string? ParentTaxonName { get; set; }
        public string? ConservationStatus { get; set; }
        public bool? Edible { get; set; }
    }
}
