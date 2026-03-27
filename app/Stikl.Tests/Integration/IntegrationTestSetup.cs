using DotNet.Testcontainers.Builders;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace Stikl.Tests.Integration;

/// <summary>
/// Starts a single Postgres container for the entire integration test run.
/// All integration test fixtures share one container; each test truncates
/// the tables it needs in [SetUp] to keep tests isolated.
/// </summary>
[SetUpFixture]
public class IntegrationTestSetup
{
    // Note: stikl.chat_event has a missing comma in the production SQL file
    // (between `kind TEXT NOT NULL` and `payload TEXT NOT NULL`). The corrected
    // version is used here.
    internal const string Schema = """
        CREATE SCHEMA IF NOT EXISTS stikl;

        CREATE TABLE IF NOT EXISTS stikl.user_event (
          username  TEXT    NOT NULL,
          version   INTEGER NOT NULL CONSTRAINT positive_version CHECK (version > 0),
          timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
          kind      TEXT    NOT NULL,
          payload   JSONB   NOT NULL,
          PRIMARY KEY (username, version)
        );

        CREATE TABLE IF NOT EXISTS stikl.readmodel_user (
          username TEXT    NOT NULL,
          email    TEXT    NOT NULL,
          version  INTEGER NOT NULL CONSTRAINT positive_version CHECK (version > 0),
          payload  JSONB   NOT NULL,
          PRIMARY KEY (username)
        );

        CREATE TABLE IF NOT EXISTS stikl.chat_event (
          pk        INTEGER PRIMARY KEY GENERATED ALWAYS AS IDENTITY NOT NULL,
          sender    TEXT    NOT NULL,
          recipient TEXT    NOT NULL,
          timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
          kind      TEXT    NOT NULL,
          payload   TEXT    NOT NULL
        );

        CREATE TABLE IF NOT EXISTS perenual_species (
          perenual_id     INTEGER PRIMARY KEY NOT NULL,
          common_name     TEXT    NOT NULL,
          scientific_name TEXT[]  NOT NULL,
          other_name      TEXT[]  NOT NULL,
          family          TEXT    NULL,
          cultivar        TEXT    NULL,
          variety         TEXT    NULL,
          species_epithet TEXT    NULL,
          genus           TEXT    NULL,
          subspecies      TEXT    NULL,
          img_regular_url TEXT    NULL,
          img_small_url   TEXT    NULL
        );

        CREATE TABLE IF NOT EXISTS wiki_species_info (
          perenual_id              INTEGER NOT NULL REFERENCES perenual_species(perenual_id),
          lang                     TEXT    NOT NULL,
          wikipedia_title          TEXT    NULL,
          wikipedia_page_url       TEXT    NULL,
          wikipedia_page_id        INTEGER NULL,
          wikidata_id              TEXT    NULL,
          description              TEXT    NULL,
          extract                  TEXT    NULL,
          common_name              TEXT    NULL,
          edible                   BOOLEAN NULL,
          hardiness_zones          TEXT    NULL,
          conservation_status      TEXT    NULL,
          taxon_rank               TEXT    NULL,
          gbif_taxon_id            TEXT    NULL,
          parent_taxon_name        TEXT    NULL,
          parent_taxon_wikidata_id TEXT    NULL,
          scraped_at               TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
          PRIMARY KEY (perenual_id, lang)
        );
        """;

    static PostgreSqlContainer? _container;

    internal static string ConnectionString =>
        _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Container not started");

    [OneTimeSetUp]
    public async Task StartContainer()
    {
        _container = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();

        await _container.StartAsync();

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(Schema, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    [OneTimeTearDown]
    public async Task StopContainer()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
