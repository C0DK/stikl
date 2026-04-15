using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Npgsql;
using NpgsqlTypes;

namespace Stikl.Web.Data;

/// <summary>
/// Scrapes Wikipedia and Wikidata for each entry in perenual_species and stores the
/// results in wiki_species_info — one row per (species, language).
///
/// Per-language data: Wikipedia description, intro extract, common name (Wikidata P1843),
/// page URL and title.
///
/// Language-agnostic Wikidata data (stored in every row): edibility (P279/P31 type
/// hierarchy), USDA hardiness zones (regex on article text), IUCN conservation status
/// (P141), taxon rank (P105), GBIF taxon ID (P846), and parent taxon (P171) for
/// species-tree navigation.
///
/// Run via: WIKI_SCRAPE=true dotnet run
/// </summary>
public partial class WikipediaScraper(NpgsqlConnection connection, HttpClient http, ILogger logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly string[] SupportedLanguages = ["en", "da"];

    // IUCN conservation status Wikidata Q-IDs → human-readable labels
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

    // Taxon rank Wikidata Q-IDs → labels (hardcoded to avoid extra API calls per species)
    private static readonly Dictionary<string, string> TaxonRankLabels = new()
    {
        ["Q7432"] = "species",
        ["Q34740"] = "genus",
        ["Q35409"] = "family",
        ["Q36602"] = "order",
        ["Q37517"] = "class",
        ["Q38348"] = "phylum",
        ["Q36244"] = "kingdom",
        ["Q68947"] = "variety",
        ["Q767728"] = "subspecies",
        ["Q4886"] = "cultivar",
        ["Q310890"] = "section",
        ["Q1065111"] = "series",
        ["Q2455704"] = "tribe",
        ["Q713623"] = "subgenus",
        ["Q3025161"] = "subvariety",
        ["Q2111609"] = "form",
    };

    // Wikidata Q-IDs whose presence in P279/P31 implies the plant is edible
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
            // Be a polite citizen: ~2 req/s to Wikipedia/Wikidata
            await Task.Delay(500, cancellationToken);
        }

        logger.Information("Wikipedia scraping completed");
    }

    // Returns species that are missing an 'en' row — we process all languages at once per species
    private async Task<List<(int Id, string ScientificName)>> GetUnscrapedSpecies(
        CancellationToken ct
    )
    {
        await using var cmd = new NpgsqlCommand(
            """
            SELECT p.perenual_id, p.scientific_name[1]
            FROM perenual_species p
            WHERE p.scientific_name[1] IS NOT NULL
              AND NOT EXISTS (
                  SELECT 1 FROM wiki_species_info w
                  WHERE w.perenual_id = p.perenual_id AND w.lang = 'en'
              )
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

    // Fetches Wikipedia/Wikidata data for one scientific name and returns one row per
    // supported language. Rows with a null WikipediaTitle mean no article was found.
    // Does not touch the database.
    public async Task<IReadOnlyList<WikiSpeciesRow>> FetchSpeciesRows(
        string scientificName,
        CancellationToken ct = default
    )
    {
        // 1. Search English Wikipedia by scientific name
        var enTitle = await SearchWikipedia("en", scientificName, ct);
        if (enTitle is null)
            return SupportedLanguages.Select(lang => new WikiSpeciesRow(lang)).ToList();

        // 2. Fetch English summary → gives us the Wikidata item ID
        var enSummary = await GetSummary("en", enTitle, ct);

        // 3. Fetch Wikidata (claims + sitelinks) once for all languages
        WikidataFull? wikidata = null;
        if (enSummary?.WikidataItem is not null)
        {
            await Task.Delay(300, ct);
            wikidata = await GetWikidataFull(enSummary.WikidataItem, ct);
        }

        // 4. Build a row for each supported language
        var rows = new List<WikiSpeciesRow>();
        foreach (var lang in SupportedLanguages)
        {
            SummaryResponse? summary;
            if (lang == "en")
            {
                summary = enSummary;
            }
            else
            {
                // Use Wikidata sitelinks to find the foreign-language Wikipedia title
                var foreignTitle = wikidata?.Sitelinks.GetValueOrDefault($"{lang}wiki")?.Title;
                if (foreignTitle is null)
                {
                    rows.Add(new WikiSpeciesRow(lang));
                    continue;
                }
                await Task.Delay(300, ct);
                summary = await GetSummary(lang, foreignTitle, ct);
            }

            if (summary is null)
            {
                rows.Add(new WikiSpeciesRow(lang));
                continue;
            }

            var pageUrl = summary.Title is not null
                ? $"https://{lang}.wikipedia.org/wiki/{Uri.EscapeDataString(summary.Title.Replace(" ", "_"))}"
                : null;

            rows.Add(
                new WikiSpeciesRow(lang)
                {
                    WikipediaTitle = summary.Title,
                    WikipediaPageUrl = pageUrl,
                    WikipediaPageId = summary.PageId,
                    WikidataId = summary.WikidataItem,
                    Description = summary.Description,
                    Extract = summary.Extract,
                    CommonName = wikidata?.CommonNames.GetValueOrDefault(lang),
                    Edible = wikidata?.Edible,
                    HardinessZones = ExtractHardinessZones(summary.Extract),
                    ConservationStatus = wikidata?.ConservationStatus,
                    TaxonRank = wikidata?.TaxonRank,
                    GbifTaxonId = wikidata?.GbifTaxonId,
                    ParentTaxonName = wikidata?.ParentTaxonName,
                    ParentTaxonWikidataId = wikidata?.ParentTaxonWikidataId,
                }
            );
        }

        return rows;
    }

    private async ValueTask ScrapeOne(int perenualId, string scientificName, CancellationToken ct)
    {
        logger
            .ForContext("perenualId", perenualId)
            .ForContext("scientificName", scientificName)
            .Debug("Scraping Wikipedia");
        try
        {
            var rows = await FetchSpeciesRows(scientificName, ct);
            foreach (var row in rows)
            {
                if (row.WikipediaTitle is null)
                    await InsertEmpty(perenualId, row.Lang, ct);
                else
                    await Insert(perenualId, row, ct);
            }
            logger.ForContext("perenualId", perenualId).Debug("Wikipedia scrape complete");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger
                .ForContext("perenualId", perenualId)
                .Warning(ex, "Wikipedia scrape failed, inserting empty records");
            foreach (var lang in SupportedLanguages)
                await InsertEmpty(perenualId, lang, ct);
        }
    }

    // Search Wikipedia in the given language; returns page title of top result
    private async Task<string?> SearchWikipedia(string lang, string query, CancellationToken ct)
    {
        var encoded = Uri.EscapeDataString(query);
        var url =
            $"https://{lang}.wikipedia.org/w/api.php?action=query&list=search&srsearch={encoded}&srlimit=1&srnamespace=0&format=json&utf8=";
        var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
            return null;
        var doc = await resp.Content.ReadFromJsonAsync<WikiSearchResponse>(JsonOptions, ct);
        return doc?.Query?.Search?.FirstOrDefault()?.Title;
    }

    // Wikipedia REST summary: description, extract, wikidata_item, pageid
    private async Task<SummaryResponse?> GetSummary(string lang, string title, CancellationToken ct)
    {
        var encoded = Uri.EscapeDataString(title.Replace(" ", "_"));
        var resp = await http.GetAsync(
            $"https://{lang}.wikipedia.org/api/rest_v1/page/summary/{encoded}",
            ct
        );
        if (!resp.IsSuccessStatusCode)
            return null;
        return await resp.Content.ReadFromJsonAsync<SummaryResponse>(JsonOptions, ct);
    }

    private async Task<WikidataFull?> GetWikidataFull(string qid, CancellationToken ct)
    {
        // Fetch claims + sitelinks in one request
        var url =
            $"https://www.wikidata.org/w/api.php?action=wbgetentities&ids={qid}&props=claims|sitelinks&format=json";
        var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
            return null;

        var doc = await resp.Content.ReadFromJsonAsync<WdEntitiesResponse>(JsonOptions, ct);
        if (doc?.Entities is null || !doc.Entities.TryGetValue(qid, out var entity))
            return null;

        var result = new WikidataFull { Sitelinks = entity.Sitelinks ?? [] };

        if (entity.Claims is null)
            return result;

        // P171: parent taxon (resolves label via a second Wikidata call)
        var parentQid = GetFirstEntityId(entity.Claims, "P171");
        if (parentQid is not null)
        {
            result.ParentTaxonWikidataId = parentQid;
            await Task.Delay(200, ct);
            result.ParentTaxonName = await GetEntityLabel(parentQid, ct);
        }

        // P141: IUCN conservation status
        var consQid = GetFirstEntityId(entity.Claims, "P141");
        if (consQid is not null && ConservationStatusLabels.TryGetValue(consQid, out var consLabel))
            result.ConservationStatus = consLabel;

        // P105: taxon rank (hardcoded label map — no extra API call needed)
        var rankQid = GetFirstEntityId(entity.Claims, "P105");
        if (rankQid is not null && TaxonRankLabels.TryGetValue(rankQid, out var rankLabel))
            result.TaxonRank = rankLabel;

        // P846: GBIF taxon ID (plain string value)
        result.GbifTaxonId = GetFirstStringValue(entity.Claims, "P846");

        // P1843: taxon common name (monolingualtext — one entry per language)
        if (entity.Claims.TryGetValue("P1843", out var commonNameClaims))
        {
            foreach (var claim in commonNameClaims)
            {
                var dv = claim.Mainsnak?.Datavalue;
                if (dv?.Type != "monolingualtext")
                    continue;
                var text = dv.Value.TryGetProperty("text", out var t) ? t.GetString() : null;
                var nameLang = dv.Value.TryGetProperty("language", out var l)
                    ? l.GetString()
                    : null;
                if (
                    text is not null
                    && nameLang is not null
                    && !result.CommonNames.ContainsKey(nameLang)
                )
                    result.CommonNames[nameLang] = text;
            }
        }

        // P279 (subclass of) / P31 (instance of): derive edibility
        var typeIds = entity
            .Claims.Where(c => c.Key is "P279" or "P31")
            .SelectMany(c => c.Value)
            .Select(c => GetEntityId(c.Mainsnak?.Datavalue))
            .OfType<string>()
            .ToHashSet();
        if (typeIds.Overlaps(EdibleQIds))
            result.Edible = true;

        return result;
    }

    // Fetch the English label for any Wikidata entity (used for parent taxon name)
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

    // Helpers for extracting typed values from polymorphic WdDatavalue.Value (JsonElement)

    private static string? GetFirstEntityId(
        Dictionary<string, WdClaim[]> claims,
        string property
    ) =>
        claims.TryGetValue(property, out var cs)
            ? GetEntityId(cs.FirstOrDefault()?.Mainsnak?.Datavalue)
            : null;

    private static string? GetFirstStringValue(
        Dictionary<string, WdClaim[]> claims,
        string property
    ) =>
        claims.TryGetValue(property, out var cs)
            ? GetStringValue(cs.FirstOrDefault()?.Mainsnak?.Datavalue)
            : null;

    private static string? GetEntityId(WdDatavalue? dv)
    {
        if (dv?.Type != "wikibase-entityid")
            return null;
        return dv.Value.TryGetProperty("id", out var p) ? p.GetString() : null;
    }

    private static string? GetStringValue(WdDatavalue? dv)
    {
        if (dv?.Type != "string")
            return null;
        return dv.Value.ValueKind == JsonValueKind.String ? dv.Value.GetString() : null;
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

    private async ValueTask Insert(int perenualId, WikiSpeciesRow row, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO wiki_species_info (
                perenual_id, lang,
                wikipedia_title, wikipedia_page_url, wikipedia_page_id, wikidata_id,
                description, extract, common_name,
                edible, hardiness_zones, conservation_status,
                taxon_rank, gbif_taxon_id,
                parent_taxon_name, parent_taxon_wikidata_id
            ) VALUES (
                @perenualId, @lang,
                @title, @pageUrl, @pageId, @wikidataId,
                @description, @extract, @commonName,
                @edible, @hardinessZones, @conservationStatus,
                @taxonRank, @gbifTaxonId,
                @parentTaxonName, @parentTaxonWikidataId
            ) ON CONFLICT (perenual_id, lang) DO UPDATE SET
                wikipedia_title          = EXCLUDED.wikipedia_title,
                wikipedia_page_url       = EXCLUDED.wikipedia_page_url,
                wikipedia_page_id        = EXCLUDED.wikipedia_page_id,
                wikidata_id              = EXCLUDED.wikidata_id,
                description              = EXCLUDED.description,
                extract                  = EXCLUDED.extract,
                common_name              = EXCLUDED.common_name,
                edible                   = EXCLUDED.edible,
                hardiness_zones          = EXCLUDED.hardiness_zones,
                conservation_status      = EXCLUDED.conservation_status,
                taxon_rank               = EXCLUDED.taxon_rank,
                gbif_taxon_id            = EXCLUDED.gbif_taxon_id,
                parent_taxon_name        = EXCLUDED.parent_taxon_name,
                parent_taxon_wikidata_id = EXCLUDED.parent_taxon_wikidata_id,
                scraped_at               = now()
            """,
            connection
        );

        cmd.Parameters.AddWithValue("perenualId", perenualId);
        AddText(cmd, "lang", row.Lang);
        AddText(cmd, "title", row.WikipediaTitle);
        AddText(cmd, "pageUrl", row.WikipediaPageUrl);
        AddInt(cmd, "pageId", row.WikipediaPageId);
        AddText(cmd, "wikidataId", row.WikidataId);
        AddText(cmd, "description", row.Description);
        AddText(cmd, "extract", row.Extract);
        AddText(cmd, "commonName", row.CommonName);
        AddBool(cmd, "edible", row.Edible);
        AddText(cmd, "hardinessZones", row.HardinessZones);
        AddText(cmd, "conservationStatus", row.ConservationStatus);
        AddText(cmd, "taxonRank", row.TaxonRank);
        AddText(cmd, "gbifTaxonId", row.GbifTaxonId);
        AddText(cmd, "parentTaxonName", row.ParentTaxonName);
        AddText(cmd, "parentTaxonWikidataId", row.ParentTaxonWikidataId);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async ValueTask InsertEmpty(int perenualId, string lang, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO wiki_species_info (perenual_id, lang) VALUES (@id, @lang) ON CONFLICT DO NOTHING",
            connection
        );
        cmd.Parameters.AddWithValue("id", perenualId);
        cmd.Parameters.AddWithValue("lang", lang);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static void AddText(NpgsqlCommand cmd, string name, string? value) =>
        cmd.Parameters.Add(name, NpgsqlDbType.Text).Value = (object?)value ?? DBNull.Value;

    private static void AddInt(NpgsqlCommand cmd, string name, int? value) =>
        cmd.Parameters.Add(name, NpgsqlDbType.Integer).Value = (object?)value ?? DBNull.Value;

    private static void AddBool(NpgsqlCommand cmd, string name, bool? value) =>
        cmd.Parameters.Add(name, NpgsqlDbType.Boolean).Value = (object?)value ?? DBNull.Value;

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
        [property: JsonPropertyName("wikibase_item")] string? WikidataItem
    );

    private record WdEntitiesResponse(
        [property: JsonPropertyName("entities")] Dictionary<string, WdEntity>? Entities
    );

    private record WdEntity(
        [property: JsonPropertyName("claims")] Dictionary<string, WdClaim[]>? Claims,
        [property: JsonPropertyName("labels")] Dictionary<string, WdLabel>? Labels,
        [property: JsonPropertyName("sitelinks")] Dictionary<string, WdSitelink>? Sitelinks
    );

    private record WdSitelink([property: JsonPropertyName("title")] string? Title);

    private record WdClaim([property: JsonPropertyName("mainsnak")] WdMainsnak? Mainsnak);

    private record WdMainsnak([property: JsonPropertyName("datavalue")] WdDatavalue? Datavalue);

    // Value is polymorphic (entity-id object, monolingualtext object, or plain string)
    // so we keep it as JsonElement and extract via type-checked helpers
    private record WdDatavalue(
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("value")] JsonElement Value
    );

    private record WdLabel([property: JsonPropertyName("value")] string? Value);

    private sealed class WikidataFull
    {
        public string? ParentTaxonWikidataId { get; set; }
        public string? ParentTaxonName { get; set; }
        public string? ConservationStatus { get; set; }
        public string? TaxonRank { get; set; }
        public string? GbifTaxonId { get; set; }
        public bool? Edible { get; set; }

        // P1843 common names keyed by ISO 639-1 language code
        public Dictionary<string, string> CommonNames { get; set; } = [];

        // Wikidata sitelinks keyed by site name (e.g. "enwiki", "dawiki")
        public Dictionary<string, WdSitelink> Sitelinks { get; set; } = [];
    }
}

/// <summary>
/// Scraped data for a single (species, language) pair.
/// A null WikipediaTitle means no Wikipedia article was found for this language.
/// </summary>
public class WikiSpeciesRow(string lang)
{
    public string Lang { get; } = lang;
    public string? WikipediaTitle { get; init; }
    public string? WikipediaPageUrl { get; init; }
    public int? WikipediaPageId { get; init; }
    public string? WikidataId { get; init; }
    public string? Description { get; init; }
    public string? Extract { get; init; }
    public string? CommonName { get; init; }
    public bool? Edible { get; init; }
    public string? HardinessZones { get; init; }
    public string? ConservationStatus { get; init; }
    public string? TaxonRank { get; init; }
    public string? GbifTaxonId { get; init; }
    public string? ParentTaxonName { get; init; }
    public string? ParentTaxonWikidataId { get; init; }
}
