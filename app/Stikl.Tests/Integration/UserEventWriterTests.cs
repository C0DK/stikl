using Npgsql;
using NUnit.Framework;
using Stikl.Web.DataAccess;
using Stikl.Web.Model;

namespace Stikl.Tests.Integration;

[TestFixture]
[Category("Integration")]
public class UserEventWriterTests
{
    NpgsqlConnection _conn = null!;
    UserEventWriter _writer = null!;

    static readonly Username Alice = Username.Parse("alice");
    static readonly Email AliceEmail = Email.Parse("alice@example.com");

    [SetUp]
    public async Task SetUp()
    {
        _conn = new NpgsqlConnection(IntegrationTestSetup.ConnectionString);
        await _conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "TRUNCATE stikl.user_event, stikl.readmodel_user",
            _conn
        );
        await cmd.ExecuteNonQueryAsync();
        _writer = new UserEventWriter(_conn);
    }

    [TearDown]
    public async Task TearDown() => await _conn.DisposeAsync();

    // ── Explicit-version overload ──────────────────────────────────────────

    [Test]
    public async Task Write_ExplicitVersion_InsertsEventAndReturnsUser()
    {
        var payload = UserCreatedPayload();
        var user = await _writer.Write(Alice, 1, payload, CancellationToken.None);

        Assert.That(user.UserName, Is.EqualTo(Alice));
        Assert.That(user.Email, Is.EqualTo(AliceEmail));
        Assert.That(user.FirstName, Is.EqualTo("Alice"));
    }

    [Test]
    public async Task Write_ExplicitVersion_DuplicateVersion_ThrowsEventBrokeConstraint()
    {
        await _writer.Write(Alice, 1, UserCreatedPayload(), CancellationToken.None);

        Assert.ThrowsAsync<UserEventWriter.EventBrokeConstraint>(
            () => _writer.Write(Alice, 1, new WantPlant(new SpeciesId(1)), CancellationToken.None).AsTask()
        );
    }

    [Test]
    public async Task Write_ExplicitVersion_SequentialVersions_Succeeds()
    {
        await _writer.Write(Alice, 1, UserCreatedPayload(), CancellationToken.None);
        var user = await _writer.Write(Alice, 2, new WantPlant(new SpeciesId(42)), CancellationToken.None);

        Assert.That(user.Wants, Contains.Item(new SpeciesId(42)));
        Assert.That(user.History, Has.Length.EqualTo(2));
    }

    // ── Auto-version overload ──────────────────────────────────────────────

    [Test]
    public async Task Write_AutoVersion_FirstEvent_AssignsVersion1()
    {
        // Auto-version: SELECT max(version)+1 → null+1 = null, so first insert needs
        // the explicit-version path to seed. Use explicit for creation then auto for second.
        await _writer.Write(Alice, 1, UserCreatedPayload(), CancellationToken.None);
        var user = await _writer.Write(Alice, new WantPlant(new SpeciesId(5)), CancellationToken.None);

        Assert.That(user.Wants, Contains.Item(new SpeciesId(5)));
    }

    [Test]
    public async Task Write_AutoVersion_MultipleEvents_IncrementsVersion()
    {
        await _writer.Write(Alice, 1, UserCreatedPayload(), CancellationToken.None);
        await _writer.Write(Alice, new WantPlant(new SpeciesId(1)), CancellationToken.None);
        var user = await _writer.Write(Alice, new WantPlant(new SpeciesId(2)), CancellationToken.None);

        Assert.That(user.History, Has.Length.EqualTo(3));
        Assert.That(user.Wants, Has.Count.EqualTo(2));
    }

    // ── Event sourcing state ───────────────────────────────────────────────

    [Test]
    public async Task Write_WantThenUnwant_UserReflectsState()
    {
        var speciesId = new SpeciesId(99);
        await _writer.Write(Alice, 1, UserCreatedPayload(), CancellationToken.None);
        await _writer.Write(Alice, 2, new WantPlant(speciesId), CancellationToken.None);
        var user = await _writer.Write(Alice, 3, new UnwantPlant(speciesId), CancellationToken.None);

        Assert.That(user.Wants, Is.Empty);
    }

    // ── Helper ────────────────────────────────────────────────────────────

    static UserCreated UserCreatedPayload() =>
        new(
            Email: AliceEmail,
            FirstName: "Alice",
            LastName: "Smith",
            Location: new LocationIQClient.Location(
                PlaceId: "1",
                OsmId: "1",
                OsmType: "N",
                Licence: "",
                Lat: "0",
                Lon: "0",
                Boundingbox: [],
                Class: "place",
                Type: "city",
                DisplayName: "Testville",
                Address: new LocationIQClient.Address("Testland", "tl", Name: "Testville")
            )
        );
}
