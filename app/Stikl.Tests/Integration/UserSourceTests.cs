using Npgsql;
using NUnit.Framework;
using Stikl.Web.DataAccess;
using Stikl.Web.Model;

namespace Stikl.Tests.Integration;

[Category("Integration")]
public class UserSourceTests
{
    NpgsqlConnection _conn = null!;
    UserSource _source = null!;
    UserEventWriter _writer = null!;

    static readonly Username Alice = Username.Parse("alice");
    static readonly Username Bob = Username.Parse("bob");
    static readonly Email AliceEmail = Email.Parse("alice@example.com");
    static readonly Email BobEmail = Email.Parse("bob@example.com");

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
        _source = new UserSource(_conn);
        _writer = new UserEventWriter(_conn);
    }

    [TearDown]
    public async Task TearDown() => await _conn.DisposeAsync();

    static LocationIQClient.Location TestLocation() =>
        new(
            "1",
            "1",
            "N",
            "",
            "0",
            "0",
            [],
            "place",
            "city",
            "Testville",
            new LocationIQClient.Address("Testland", "tl", Name: "Testville")
        );

    static UserCreated AliceCreated() => new(AliceEmail, "Alice", "Smith", TestLocation());

    static UserCreated BobCreated() => new(BobEmail, "Bob", "Jones", TestLocation());

    async Task InsertEventDirectly(Username username, int version, string kind, string payload)
    {
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO stikl.user_event(username, version, kind, payload) VALUES($1, $2, $3, $4::jsonb)",
            _conn
        );
        cmd.Parameters.AddWithValue(username.Value);
        cmd.Parameters.AddWithValue(version);
        cmd.Parameters.AddWithValue(kind);
        cmd.Parameters.AddWithValue(payload);
        await cmd.ExecuteNonQueryAsync();
    }

    [TestFixture]
    public class GetByUsername : UserSourceTests
    {
        [Test]
        public async Task MissingUser_ReturnsNull()
        {
            Assert.That(await _source.GetOrNull(Alice, CancellationToken.None), Is.Null);
        }

        [Test]
        public async Task AfterWrite_ReturnsUser()
        {
            await _writer.Write(Alice, 1, AliceCreated(), CancellationToken.None);

            var user = await _source.GetOrNull(Alice, CancellationToken.None);

            Assert.That(user, Is.Not.Null);
            Assert.That(user!.UserName, Is.EqualTo(Alice));
            Assert.That(user.Email, Is.EqualTo(AliceEmail));
        }
    }

    [TestFixture]
    public class GetByEmail : UserSourceTests
    {
        [Test]
        public async Task MissingUser_ReturnsNull()
        {
            Assert.That(await _source.GetOrNull(AliceEmail, CancellationToken.None), Is.Null);
        }

        [Test]
        public async Task AfterWrite_ReturnsUser()
        {
            await _writer.Write(Alice, 1, AliceCreated(), CancellationToken.None);

            var user = await _source.GetOrNull(AliceEmail, CancellationToken.None);

            Assert.That(user, Is.Not.Null);
            Assert.That(user!.UserName, Is.EqualTo(Alice));
        }

        [Test]
        public async Task DifferentEmail_ReturnsNull()
        {
            await _writer.Write(Alice, 1, AliceCreated(), CancellationToken.None);

            Assert.That(await _source.GetOrNull(BobEmail, CancellationToken.None), Is.Null);
        }
    }

    [TestFixture]
    public class Refresh : UserSourceTests
    {
        [Test]
        public async Task RebuildsUserFromEventStream()
        {
            await InsertEventDirectly(Alice, 1, "user_created", AliceCreated().Serialize());
            await InsertEventDirectly(
                Alice,
                2,
                "want_plant",
                new WantPlant(new SpeciesId(7)).Serialize()
            );

            var user = await _source.Refresh(Alice, CancellationToken.None);

            Assert.That(user.UserName, Is.EqualTo(Alice));
            Assert.That(user.Wants, Contains.Item(new SpeciesId(7)));
            Assert.That(user.History, Has.Length.EqualTo(2));
        }

        [Test]
        public async Task UpdatesReadmodelWithLatestVersion()
        {
            await _writer.Write(Alice, 1, AliceCreated(), CancellationToken.None);
            await InsertEventDirectly(
                Alice,
                2,
                "want_plant",
                new WantPlant(new SpeciesId(3)).Serialize()
            );

            await _source.Refresh(Alice, CancellationToken.None);

            var user = await _source.GetOrNull(Alice, CancellationToken.None);
            Assert.That(user!.Wants, Contains.Item(new SpeciesId(3)));
        }

        [Test]
        public async Task NoEvents_Throws()
        {
            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _source.Refresh(Alice, CancellationToken.None).AsTask()
            );
        }

        [Test]
        public async Task MultipleUsers_AreIsolated()
        {
            await _writer.Write(Alice, 1, AliceCreated(), CancellationToken.None);
            await _writer.Write(Bob, 1, BobCreated(), CancellationToken.None);
            await _writer.Write(Alice, 2, new WantPlant(new SpeciesId(10)), CancellationToken.None);

            var alice = await _source.GetOrNull(Alice, CancellationToken.None);
            var bob = await _source.GetOrNull(Bob, CancellationToken.None);

            Assert.That(alice!.Wants, Contains.Item(new SpeciesId(10)));
            Assert.That(bob!.Wants, Is.Empty);
        }
    }
}
