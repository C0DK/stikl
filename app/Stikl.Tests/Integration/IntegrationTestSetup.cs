using DotNet.Testcontainers.Builders;
using NUnit.Framework;
using Npgsql;
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
        """;

    static PostgreSqlContainer? _container;

    internal static string ConnectionString =>
        _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Container not started");

    [OneTimeSetUp]
    public async Task StartContainer()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

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
