using Npgsql;
using NUnit.Framework;
using Stikl.Web.DataAccess;
using Stikl.Web.Model;

namespace Stikl.Tests.Integration;

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

    [TestFixture]
    public class ExplicitVersion : UserEventWriterTests
    {
        [Test]
        public async Task InsertsEventAndReturnsUser()
        {
            var user = await _writer.Write(Alice, 1, UserCreatedPayload(), CancellationToken.None);

            Assert.That(user.UserName, Is.EqualTo(Alice));
            Assert.That(user.Email, Is.EqualTo(AliceEmail));
            Assert.That(user.FirstName, Is.EqualTo("Alice"));
        }

        [Test]
        public async Task DuplicateVersion_ThrowsEventBrokeConstraint()
        {
            await _writer.Write(Alice, 1, UserCreatedPayload(), CancellationToken.None);

            Assert.ThrowsAsync<UserEventWriter.EventBrokeConstraint>(() =>
                _writer
                    .Write(Alice, 1, new WantPlant(new SpeciesId(1)), CancellationToken.None)
                    .AsTask()
            );
        }

        [Test]
        public async Task SequentialVersions_Succeeds()
        {
            await _writer.Write(Alice, 1, UserCreatedPayload(), CancellationToken.None);
            var user = await _writer.Write(
                Alice,
                2,
                new WantPlant(new SpeciesId(42)),
                CancellationToken.None
            );

            Assert.That(user.Wants, Contains.Item(new SpeciesId(42)));
            Assert.That(user.History, Has.Length.EqualTo(2));
        }
    }

    [TestFixture]
    public class AutoVersion : UserEventWriterTests
    {
        [Test]
        public async Task AfterSeed_AppendsByAutoIncrement()
        {
            await _writer.Write(Alice, 1, UserCreatedPayload(), CancellationToken.None);
            var user = await _writer.Write(
                Alice,
                new WantPlant(new SpeciesId(5)),
                CancellationToken.None
            );

            Assert.That(user.Wants, Contains.Item(new SpeciesId(5)));
        }

        [Test]
        public async Task MultipleEvents_IncrementsVersion()
        {
            await _writer.Write(Alice, 1, UserCreatedPayload(), CancellationToken.None);
            await _writer.Write(Alice, new WantPlant(new SpeciesId(1)), CancellationToken.None);
            var user = await _writer.Write(
                Alice,
                new WantPlant(new SpeciesId(2)),
                CancellationToken.None
            );

            Assert.That(user.History, Has.Length.EqualTo(3));
            Assert.That(user.Wants, Has.Count.EqualTo(2));
        }
    }

    [TestFixture]
    public class EventSourcing : UserEventWriterTests
    {
        [Test]
        public async Task WantThenUnwant_UserReflectsState()
        {
            var speciesId = new SpeciesId(99);
            await _writer.Write(Alice, 1, UserCreatedPayload(), CancellationToken.None);
            await _writer.Write(Alice, 2, new WantPlant(speciesId), CancellationToken.None);
            var user = await _writer.Write(
                Alice,
                3,
                new UnwantPlant(speciesId),
                CancellationToken.None
            );

            Assert.That(user.Wants, Is.Empty);
        }
    }
}
