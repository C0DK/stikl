using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Npgsql;
using NUnit.Framework;
using Stikl.Web.DataAccess;
using Stikl.Web.Model;

namespace Stikl.Tests.Integration;

/// <summary>
/// Integration tests for ChatStore.
///
/// ChatStore takes an HttpContext to resolve the current user (via ClaimTypes.Name).
/// We supply a DefaultHttpContext with a ClaimsIdentity so no ASP.NET pipeline is needed.
///
/// The complex SQL being exercised:
///   - AnyUnread: GREATEST/LEAST grouping, nested read-timestamp subquery
///   - ListConversations: MAX(pk) per conversation pair, unread flag
///   - UpdateRead: conditional insert only when newer messages exist
/// </summary>
[Category("Integration")]
public class ChatStoreTests
{
    NpgsqlConnection _conn = null!;

    static readonly Username Alice = Username.Parse("alice");
    static readonly Username Bob = Username.Parse("bob");
    static readonly Username Carol = Username.Parse("carol");

    [SetUp]
    public async Task SetUp()
    {
        _conn = new NpgsqlConnection(IntegrationTestSetup.ConnectionString);
        await _conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("TRUNCATE stikl.chat_event", _conn);
        await cmd.ExecuteNonQueryAsync();
    }

    [TearDown]
    public async Task TearDown() => await _conn.DisposeAsync();

    ChatStore StoreFor(Username username)
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, username.Value)],
                authenticationType: "test"
            )
        );
        return new ChatStore(_conn, ctx);
    }

    async Task<int> CountRows()
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT COUNT(*)::int FROM stikl.chat_event",
            _conn
        );
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    [TestFixture]
    public class SendMessage : ChatStoreTests
    {
        [Test]
        public async Task InsertsMessageIntoDatabase()
        {
            await StoreFor(Alice).SendMessage(Bob, "Hello Bob!", CancellationToken.None);

            Assert.That(await CountRows(), Is.EqualTo(1));
        }

        [Test]
        public async Task MessageIsReturnedInReadAll()
        {
            await StoreFor(Alice).SendMessage(Bob, "Hi there", CancellationToken.None);

            var messages = await StoreFor(Bob).ReadAll(Alice, CancellationToken.None).ToListAsync();

            var msg = messages.OfType<ChatEvent>().First(e => e.Payload is Message);
            Assert.That(((Message)msg.Payload).Content, Is.EqualTo("Hi there"));
        }
    }

    [TestFixture]
    public class AnyUnread : ChatStoreTests
    {
        [Test]
        public async Task NoMessages_ReturnsFalse()
        {
            Assert.That(await StoreFor(Alice).AnyUnread(CancellationToken.None), Is.False);
        }

        [Test]
        public async Task ReceivedMessage_ReturnsTrue()
        {
            await StoreFor(Bob).SendMessage(Alice, "Hey Alice", CancellationToken.None);

            Assert.That(await StoreFor(Alice).AnyUnread(CancellationToken.None), Is.True);
        }

        [Test]
        public async Task AfterReading_ReturnsFalse()
        {
            await StoreFor(Bob).SendMessage(Alice, "Hey Alice", CancellationToken.None);
            await StoreFor(Alice).ReadAll(Bob, CancellationToken.None).ToListAsync();

            Assert.That(await StoreFor(Alice).AnyUnread(CancellationToken.None), Is.False);
        }

        [Test]
        public async Task SentMessageToOther_ReturnsFalse()
        {
            await StoreFor(Alice).SendMessage(Bob, "Hey Bob", CancellationToken.None);

            Assert.That(await StoreFor(Alice).AnyUnread(CancellationToken.None), Is.False);
        }

        [Test]
        public async Task NewMessageAfterRead_ReturnsTrue()
        {
            await StoreFor(Bob).SendMessage(Alice, "First message", CancellationToken.None);
            await StoreFor(Alice).ReadAll(Bob, CancellationToken.None).ToListAsync();
            await StoreFor(Bob).SendMessage(Alice, "Second message", CancellationToken.None);

            Assert.That(await StoreFor(Alice).AnyUnread(CancellationToken.None), Is.True);
        }
    }

    [TestFixture]
    public class ListConversations : ChatStoreTests
    {
        [Test]
        public async Task NoMessages_ReturnsEmpty()
        {
            var conversations = await StoreFor(Alice)
                .ListConversations(CancellationToken.None)
                .ToListAsync();
            Assert.That(conversations, Is.Empty);
        }

        [Test]
        public async Task OneConversation_ReturnsSingleEntry()
        {
            await StoreFor(Alice).SendMessage(Bob, "Hi Bob", CancellationToken.None);

            var conversations = await StoreFor(Alice)
                .ListConversations(CancellationToken.None)
                .ToListAsync();

            Assert.That(conversations, Has.Count.EqualTo(1));
            Assert.That(conversations[0].Username, Is.EqualTo(Bob));
        }

        [Test]
        public async Task MultipleMessagesInSameConversation_ReturnsSingleEntry()
        {
            await StoreFor(Alice).SendMessage(Bob, "First", CancellationToken.None);
            await StoreFor(Alice).SendMessage(Bob, "Second", CancellationToken.None);
            await StoreFor(Bob).SendMessage(Alice, "Reply", CancellationToken.None);

            var conversations = await StoreFor(Alice)
                .ListConversations(CancellationToken.None)
                .ToListAsync();

            Assert.That(conversations, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task TwoSeparateConversations_ReturnsTwoEntries()
        {
            await StoreFor(Alice).SendMessage(Bob, "Hi Bob", CancellationToken.None);
            await StoreFor(Alice).SendMessage(Carol, "Hi Carol", CancellationToken.None);

            var conversations = await StoreFor(Alice)
                .ListConversations(CancellationToken.None)
                .ToListAsync();

            Assert.That(conversations, Has.Count.EqualTo(2));
            var partners = conversations.Select(c => c.Username).ToHashSet();
            Assert.That(partners, Contains.Item(Bob));
            Assert.That(partners, Contains.Item(Carol));
        }

        [Test]
        public async Task ReceivedUnreadMessage_MarkedAsUnread()
        {
            await StoreFor(Bob).SendMessage(Alice, "Unread message", CancellationToken.None);

            var conversations = await StoreFor(Alice)
                .ListConversations(CancellationToken.None)
                .ToListAsync();

            Assert.That(conversations[0].Unread, Is.True);
        }

        [Test]
        public async Task AfterReading_MarkedAsRead()
        {
            await StoreFor(Bob).SendMessage(Alice, "A message", CancellationToken.None);
            await StoreFor(Alice).ReadAll(Bob, CancellationToken.None).ToListAsync();

            var conversations = await StoreFor(Alice)
                .ListConversations(CancellationToken.None)
                .ToListAsync();

            Assert.That(conversations[0].Unread, Is.False);
        }
    }

    [TestFixture]
    public class UpdateRead : ChatStoreTests
    {
        [Test]
        public async Task NoMessages_DoesNotInsertReadEvent()
        {
            await StoreFor(Alice).UpdateRead(Bob, CancellationToken.None);

            Assert.That(await CountRows(), Is.EqualTo(0));
        }

        [Test]
        public async Task WithUnreadMessages_InsertsReadEvent()
        {
            await StoreFor(Bob).SendMessage(Alice, "Hey", CancellationToken.None);
            await StoreFor(Alice).UpdateRead(Bob, CancellationToken.None);

            Assert.That(await CountRows(), Is.EqualTo(2)); // 1 message + 1 read event
        }

        [Test]
        public async Task CalledTwice_DoesNotInsertDuplicateReadEvent()
        {
            await StoreFor(Bob).SendMessage(Alice, "Hey", CancellationToken.None);
            await StoreFor(Alice).UpdateRead(Bob, CancellationToken.None);
            await StoreFor(Alice).UpdateRead(Bob, CancellationToken.None);

            Assert.That(await CountRows(), Is.EqualTo(2)); // still 1 message + 1 read
        }
    }

    [TestFixture]
    public class LatestChat : ChatStoreTests
    {
        [Test]
        public async Task NoMessages_ReturnsNull()
        {
            Assert.That(await StoreFor(Alice).LatestChat(CancellationToken.None), Is.Null);
        }

        [Test]
        public async Task OneConversation_ReturnsThatPartner()
        {
            await StoreFor(Alice).SendMessage(Bob, "Hi", CancellationToken.None);

            Assert.That(
                await StoreFor(Alice).LatestChat(CancellationToken.None),
                Is.EqualTo(Bob)
            );
        }

        [Test]
        public async Task TwoConversations_ReturnsMostRecentPartner()
        {
            await StoreFor(Alice).SendMessage(Bob, "Hi Bob", CancellationToken.None);
            await StoreFor(Alice).SendMessage(Carol, "Hi Carol", CancellationToken.None);

            Assert.That(
                await StoreFor(Alice).LatestChat(CancellationToken.None),
                Is.EqualTo(Carol)
            );
        }
    }
}

file static class AsyncEnumerableExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source)
            list.Add(item);
        return list;
    }
}
