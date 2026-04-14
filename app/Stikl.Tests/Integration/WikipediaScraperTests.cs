using System.Net;
using System.Text;
using Npgsql;
using NUnit.Framework;
using Serilog;
using Stikl.Web.Data;

namespace Stikl.Tests.Integration;

[Category("Integration")]
public class WikipediaScraperTests
{
    NpgsqlConnection _conn = null!;

    [SetUp]
    public async Task SetUp()
    {
        _conn = new NpgsqlConnection(IntegrationTestSetup.ConnectionString);
        await _conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("TRUNCATE perenual_species CASCADE", _conn);
        await cmd.ExecuteNonQueryAsync();
    }

    [TearDown]
    public async Task TearDown() => await _conn.DisposeAsync();

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task InsertSpecies(int id, string commonName, params string[] scientificNames)
    {
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO perenual_species(perenual_id, common_name, scientific_name, other_name) VALUES($1, $2, $3, $4)",
            _conn
        );
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(commonName);
        cmd.Parameters.AddWithValue(scientificNames);
        cmd.Parameters.AddWithValue(Array.Empty<string>());
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<WikiRow?> GetWikiRow(int perenualId, string lang)
    {
        await using var cmd = new NpgsqlCommand(
            """
            SELECT wikipedia_title, wikipedia_page_url, wikipedia_page_id, wikidata_id,
                   description, extract, common_name, edible, hardiness_zones,
                   conservation_status, taxon_rank, gbif_taxon_id,
                   parent_taxon_name, parent_taxon_wikidata_id
            FROM wiki_species_info
            WHERE perenual_id = $1 AND lang = $2
            """,
            _conn
        );
        cmd.Parameters.AddWithValue(perenualId);
        cmd.Parameters.AddWithValue(lang);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new WikiRow(
            WikipediaTitle: reader.IsDBNull(0) ? null : reader.GetString(0),
            WikipediaPageUrl: reader.IsDBNull(1) ? null : reader.GetString(1),
            WikipediaPageId: reader.IsDBNull(2) ? null : reader.GetInt32(2),
            WikidataId: reader.IsDBNull(3) ? null : reader.GetString(3),
            Description: reader.IsDBNull(4) ? null : reader.GetString(4),
            Extract: reader.IsDBNull(5) ? null : reader.GetString(5),
            CommonName: reader.IsDBNull(6) ? null : reader.GetString(6),
            Edible: reader.IsDBNull(7) ? null : reader.GetBoolean(7),
            HardinessZones: reader.IsDBNull(8) ? null : reader.GetString(8),
            ConservationStatus: reader.IsDBNull(9) ? null : reader.GetString(9),
            TaxonRank: reader.IsDBNull(10) ? null : reader.GetString(10),
            GbifTaxonId: reader.IsDBNull(11) ? null : reader.GetString(11),
            ParentTaxonName: reader.IsDBNull(12) ? null : reader.GetString(12),
            ParentTaxonWikidataId: reader.IsDBNull(13) ? null : reader.GetString(13)
        );
    }

    private static WikipediaScraper BuildScraper(
        NpgsqlConnection conn,
        FakeHttpMessageHandler handler
    ) => new WikipediaScraper(conn, new HttpClient(handler), Log.Logger);

    // Standard fake responses for a fully-populated Rosa canina entry
    private static FakeHttpMessageHandler FullHandler() =>
        new FakeHttpMessageHandler()
            .Add(
                url => url.Contains("en.wikipedia.org/w/api.php") && url.Contains("list=search"),
                """{"query":{"search":[{"title":"Rosa canina","pageid":123456}]}}"""
            )
            .Add(
                url => url.Contains("en.wikipedia.org/api/rest_v1/page/summary/Rosa_canina"),
                """
                {
                  "title": "Rosa canina",
                  "pageid": 123456,
                  "description": "species of rose",
                  "extract": "Rosa canina grows in hardiness zones 4-9 across Europe.",
                  "wikidata_item": "Q30014"
                }
                """
            )
            .Add(
                url => url.Contains("da.wikipedia.org/api/rest_v1/page/summary/Hunderose"),
                """
                {
                  "title": "Hunderose",
                  "pageid": 654321,
                  "description": "en roseart",
                  "extract": "Hunderose er en plantearten.",
                  "wikidata_item": "Q30014"
                }
                """
            )
            .Add(
                url =>
                    url.Contains("wikidata.org/w/api.php")
                    && url.Contains("Q30014")
                    && url.Contains("props=claims"),
                """
                {
                  "entities": {
                    "Q30014": {
                      "claims": {
                        "P171": [{"mainsnak":{"datavalue":{"type":"wikibase-entityid","value":{"id":"Q34740"}}}}],
                        "P141": [{"mainsnak":{"datavalue":{"type":"wikibase-entityid","value":{"id":"Q211005"}}}}],
                        "P105": [{"mainsnak":{"datavalue":{"type":"wikibase-entityid","value":{"id":"Q7432"}}}}],
                        "P846": [{"mainsnak":{"datavalue":{"type":"string","value":"3021337"}}}],
                        "P1843": [
                          {"mainsnak":{"datavalue":{"type":"monolingualtext","value":{"text":"dog rose","language":"en"}}}},
                          {"mainsnak":{"datavalue":{"type":"monolingualtext","value":{"text":"hunderose","language":"da"}}}}
                        ],
                        "P279": [{"mainsnak":{"datavalue":{"type":"wikibase-entityid","value":{"id":"Q145409"}}}}]
                      },
                      "sitelinks": {
                        "enwiki": {"title":"Rosa canina"},
                        "dawiki": {"title":"Hunderose"}
                      }
                    }
                  }
                }
                """
            )
            .Add(
                url =>
                    url.Contains("wikidata.org/w/api.php")
                    && url.Contains("Q34740")
                    && url.Contains("props=labels"),
                """{"entities":{"Q34740":{"labels":{"en":{"value":"Rosa"}}}}}"""
            );

    private record WikiRow(
        string? WikipediaTitle,
        string? WikipediaPageUrl,
        int? WikipediaPageId,
        string? WikidataId,
        string? Description,
        string? Extract,
        string? CommonName,
        bool? Edible,
        string? HardinessZones,
        string? ConservationStatus,
        string? TaxonRank,
        string? GbifTaxonId,
        string? ParentTaxonName,
        string? ParentTaxonWikidataId
    );

    // -------------------------------------------------------------------------
    // Test fixtures
    // -------------------------------------------------------------------------

    [TestFixture]
    public class WhenArticleFound : WikipediaScraperTests
    {
        [Test]
        public async Task WritesEnglishRow()
        {
            await InsertSpecies(1, "Dog Rose", "Rosa canina");
            await BuildScraper(_conn, FullHandler()).Scrape();

            var row = await GetWikiRow(1, "en");
            Assert.That(row, Is.Not.Null);
            Assert.That(row!.WikipediaTitle, Is.EqualTo("Rosa canina"));
            Assert.That(row.WikipediaPageId, Is.EqualTo(123456));
            Assert.That(row.WikidataId, Is.EqualTo("Q30014"));
            Assert.That(row.Description, Is.EqualTo("species of rose"));
            Assert.That(row.Extract, Does.Contain("Rosa canina"));
        }

        [Test]
        public async Task WikipediaPageUrlIsHumanReadable()
        {
            await InsertSpecies(1, "Dog Rose", "Rosa canina");
            await BuildScraper(_conn, FullHandler()).Scrape();

            var row = await GetWikiRow(1, "en");
            Assert.That(
                row!.WikipediaPageUrl,
                Is.EqualTo("https://en.wikipedia.org/wiki/Rosa_canina")
            );
        }

        [Test]
        public async Task WritesDanishRowViaSitelink()
        {
            await InsertSpecies(1, "Dog Rose", "Rosa canina");
            await BuildScraper(_conn, FullHandler()).Scrape();

            var row = await GetWikiRow(1, "da");
            Assert.That(row, Is.Not.Null);
            Assert.That(row!.WikipediaTitle, Is.EqualTo("Hunderose"));
            Assert.That(row.WikipediaPageId, Is.EqualTo(654321));
            Assert.That(
                row.WikipediaPageUrl,
                Is.EqualTo("https://da.wikipedia.org/wiki/Hunderose")
            );
            Assert.That(row.Description, Is.EqualTo("en roseart"));
        }

        [Test]
        public async Task PopulatesWikidataFields()
        {
            await InsertSpecies(1, "Dog Rose", "Rosa canina");
            await BuildScraper(_conn, FullHandler()).Scrape();

            var row = await GetWikiRow(1, "en");
            Assert.That(row!.TaxonRank, Is.EqualTo("species"));
            Assert.That(row.GbifTaxonId, Is.EqualTo("3021337"));
            Assert.That(row.ConservationStatus, Is.EqualTo("Least Concern"));
            Assert.That(row.ParentTaxonWikidataId, Is.EqualTo("Q34740"));
            Assert.That(row.ParentTaxonName, Is.EqualTo("Rosa"));
            Assert.That(row.Edible, Is.True);
        }

        [Test]
        public async Task ParsesHardinessZonesFromExtract()
        {
            await InsertSpecies(1, "Dog Rose", "Rosa canina");
            await BuildScraper(_conn, FullHandler()).Scrape();

            var row = await GetWikiRow(1, "en");
            Assert.That(row!.HardinessZones, Is.EqualTo("hardiness zones 4-9"));
        }

        [Test]
        public async Task StoresLanguageSpecificCommonNames()
        {
            await InsertSpecies(1, "Dog Rose", "Rosa canina");
            await BuildScraper(_conn, FullHandler()).Scrape();

            var enRow = await GetWikiRow(1, "en");
            var daRow = await GetWikiRow(1, "da");
            Assert.That(enRow!.CommonName, Is.EqualTo("dog rose"));
            Assert.That(daRow!.CommonName, Is.EqualTo("hunderose"));
        }
    }

    [TestFixture]
    public class WhenWikipediaSearchEmpty : WikipediaScraperTests
    {
        [Test]
        public async Task InsertsEmptyRowsForAllLanguages()
        {
            await InsertSpecies(1, "Unknown Plant", "Fictus unknownus");
            var handler = new FakeHttpMessageHandler().Add(
                url => url.Contains("list=search"),
                """{"query":{"search":[]}}"""
            );

            await BuildScraper(_conn, handler).Scrape();

            var enRow = await GetWikiRow(1, "en");
            var daRow = await GetWikiRow(1, "da");
            Assert.That(enRow, Is.Not.Null);
            Assert.That(enRow!.WikipediaTitle, Is.Null);
            Assert.That(daRow, Is.Not.Null);
            Assert.That(daRow!.WikipediaTitle, Is.Null);
        }
    }

    [TestFixture]
    public class WhenWikipediaReturnsError : WikipediaScraperTests
    {
        [Test]
        public async Task InsertsEmptyRowsForAllLanguages()
        {
            await InsertSpecies(1, "Dog Rose", "Rosa canina");
            // Handler returns 404 for everything
            await BuildScraper(_conn, new FakeHttpMessageHandler()).Scrape();

            var enRow = await GetWikiRow(1, "en");
            var daRow = await GetWikiRow(1, "da");
            Assert.That(enRow, Is.Not.Null);
            Assert.That(daRow, Is.Not.Null);
        }
    }

    [TestFixture]
    public class WhenNoDanishSitelink : WikipediaScraperTests
    {
        [Test]
        public async Task DanishRowIsEmptyButEnglishIsPopulated()
        {
            await InsertSpecies(1, "Dog Rose", "Rosa canina");
            var handler = new FakeHttpMessageHandler()
                .Add(
                    url =>
                        url.Contains("en.wikipedia.org/w/api.php") && url.Contains("list=search"),
                    """{"query":{"search":[{"title":"Rosa canina","pageid":1}]}}"""
                )
                .Add(
                    url => url.Contains("en.wikipedia.org/api/rest_v1/page/summary"),
                    """{"title":"Rosa canina","pageid":1,"description":"a rose","wikidata_item":"Q1"}"""
                )
                .Add(
                    url => url.Contains("Q1") && url.Contains("props=claims"),
                    // No dawiki sitelink
                    """{"entities":{"Q1":{"claims":{},"sitelinks":{"enwiki":{"title":"Rosa canina"}}}}}"""
                );

            await BuildScraper(_conn, handler).Scrape();

            var enRow = await GetWikiRow(1, "en");
            var daRow = await GetWikiRow(1, "da");
            Assert.That(enRow!.WikipediaTitle, Is.EqualTo("Rosa canina"));
            Assert.That(daRow, Is.Not.Null);
            Assert.That(daRow!.WikipediaTitle, Is.Null); // empty placeholder row
        }
    }

    [TestFixture]
    public class Idempotency : WikipediaScraperTests
    {
        [Test]
        public async Task AlreadyScrapedSpecies_IsSkippedOnRerun()
        {
            await InsertSpecies(1, "Dog Rose", "Rosa canina");
            var handler = FullHandler();

            await BuildScraper(_conn, handler).Scrape();
            var callCountAfterFirst = handler.CallCount;

            // Second run — 'en' row already exists, scraper should skip
            await BuildScraper(_conn, handler).Scrape();

            Assert.That(handler.CallCount, Is.EqualTo(callCountAfterFirst));
        }
    }

    [TestFixture]
    public class MultipleSpecies : WikipediaScraperTests
    {
        [Test]
        public async Task ScrapesEachSpeciesIndependently()
        {
            await InsertSpecies(1, "Dog Rose", "Rosa");
            await InsertSpecies(2, "Stinging Nettle", "Urtica");

            var handler = new FakeHttpMessageHandler()
                .Add(
                    url => url.Contains("list=search") && url.Contains("Rosa"),
                    """{"query":{"search":[{"title":"Rosa canina","pageid":1}]}}"""
                )
                .Add(
                    url => url.Contains("list=search") && url.Contains("Urtica"),
                    """{"query":{"search":[{"title":"Urtica dioica","pageid":2}]}}"""
                )
                .Add(
                    url => url.Contains("summary/Rosa_canina"),
                    """{"title":"Rosa canina","pageid":1,"description":"a rose","wikidata_item":"Q1"}"""
                )
                .Add(
                    url => url.Contains("summary/Urtica_dioica"),
                    """{"title":"Urtica dioica","pageid":2,"description":"a nettle","wikidata_item":"Q2"}"""
                )
                .Add(
                    url => url.Contains("wikidata.org") && url.Contains("claims"),
                    """{"entities":{"Q1":{"claims":{},"sitelinks":{}},"Q2":{"claims":{},"sitelinks":{}}}}"""
                );

            await BuildScraper(_conn, handler).Scrape();

            var rose = await GetWikiRow(1, "en");
            var nettle = await GetWikiRow(2, "en");
            Assert.That(rose!.Description, Is.EqualTo("a rose"));
            Assert.That(nettle!.Description, Is.EqualTo("a nettle"));
        }
    }
}
