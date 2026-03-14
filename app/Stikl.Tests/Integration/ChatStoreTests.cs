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
[TestFixture]
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

    // ── SendMessage ────────────────────────────────────────────────────────

    [Test]
    public async Task SendMessage_InsertsMessageIntoDatabase()
    {
        var store = StoreFor(Alice);
        await store.SendMessage(Bob, "Hello Bob!", CancellationToken.None);

        var rows = await CountRows();
        Assert.That(rows, Is.EqualTo(1));
    }

    [Test]
    public async Task SendMessage_MessageIsReturnedInReadAll()
    {
        var aliceStore = StoreFor(Alice);
        await aliceStore.SendMessage(Bob, "Hi there", CancellationToken.None);

        var bobStore = StoreFor(Bob);
        var messages = await bobStore.ReadAll(Alice, CancellationToken.None).ToListAsync();

        Assert.That(messages, Has.Count.GreaterThanOrEqualTo(1));
        var msg = messages.OfType<ChatEvent>().First(e => e.Payload is Message);
        Assert.That(((Message)msg.Payload).Content, Is.EqualTo("Hi there"));
    }

    // ── AnyUnread ──────────────────────────────────────────────────────────

    [Test]
    public async Task AnyUnread_NoMessages_ReturnsFalse()
    {
        var store = StoreFor(Alice);
        var result = await store.AnyUnread(CancellationToken.None);
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task AnyUnread_ReceivedMessage_ReturnsTrue()
    {
        // Bob sends to Alice; Alice has not read it
        await StoreFor(Bob).SendMessage(Alice, "Hey Alice", CancellationToken.None);

        var result = await StoreFor(Alice).AnyUnread(CancellationToken.None);
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task AnyUnread_AfterReading_ReturnsFalse()
    {
        await StoreFor(Bob).SendMessage(Alice, "Hey Alice", CancellationToken.None);

        var aliceStore = StoreFor(Alice);
        // ReadAll triggers UpdateRead internally
        await aliceStore.ReadAll(Bob, CancellationToken.None).ToListAsync();

        var result = await aliceStore.AnyUnread(CancellationToken.None);
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task AnyUnread_SentMessageToOther_ReturnsFalse()
    {
        // Alice sends to Bob; Alice's own sent messages should not count as unread for Alice
        await StoreFor(Alice).SendMessage(Bob, "Hey Bob", CancellationToken.None);

        var result = await StoreFor(Alice).AnyUnread(CancellationToken.None);
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task AnyUnread_NewMessageAfterRead_ReturnsTrue()
    {
        await StoreFor(Bob).SendMessage(Alice, "First message", CancellationToken.None);
        await StoreFor(Alice).ReadAll(Bob, CancellationToken.None).ToListAsync();

        // Bob sends another message after Alice read
        await StoreFor(Bob).SendMessage(Alice, "Second message", CancellationToken.None);

        var result = await StoreFor(Alice).AnyUnread(CancellationToken.None);
        Assert.That(result, Is.True);
    }

    // ── ListConversations ──────────────────────────────────────────────────

    [Test]
    public async Task ListConversations_NoMessages_ReturnsEmpty()
    {
        var conversations = await StoreFor(Alice).ListConversations(CancellationToken.None).ToListAsync();
        Assert.That(conversations, Is.Empty);
    }

    [Test]
    public async Task ListConversations_OneConversation_ReturnsSingleEntry()
    {
        await StoreFor(Alice).SendMessage(Bob, "Hi Bob", CancellationToken.None);

        var conversations = await StoreFor(Alice).ListConversations(CancellationToken.None).ToListAsync();

        Assert.That(conversations, Has.Count.EqualTo(1));
        Assert.That(conversations[0].Username, Is.EqualTo(Bob));
    }

    [Test]
    public async Task ListConversations_MultipleMessagesInSameConversation_ReturnsSingleEntry()
    {
        await StoreFor(Alice).SendMessage(Bob, "First", CancellationToken.None);
        await StoreFor(Alice).SendMessage(Bob, "Second", CancellationToken.None);
        await StoreFor(Bob).SendMessage(Alice, "Reply", CancellationToken.None);

        var conversations = await StoreFor(Alice).ListConversations(CancellationToken.None).ToListAsync();

        // All messages in the Alice↔Bob thread should be grouped into one entry
        Assert.That(conversations, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ListConversations_TwoSeparateConversations_ReturnsTwoEntries()
    {
        await StoreFor(Alice).SendMessage(Bob, "Hi Bob", CancellationToken.None);
        await StoreFor(Alice).SendMessage(Carol, "Hi Carol", CancellationToken.None);

        var conversations = await StoreFor(Alice).ListConversations(CancellationToken.None).ToListAsync();

        Assert.That(conversations, Has.Count.EqualTo(2));
        var partners = conversations.Select(c => c.Username).ToHashSet();
        Assert.That(partners, Contains.Item(Bob));
        Assert.That(partners, Contains.Item(Carol));
    }

    [Test]
    public async Task ListConversations_ReceivedUnreadMessage_MarkedAsUnread()
    {
        await StoreFor(Bob).SendMessage(Alice, "Unread message", CancellationToken.None);

        var conversations = await StoreFor(Alice).ListConversations(CancellationToken.None).ToListAsync();

        Assert.That(conversations[0].Unread, Is.True);
    }

    [Test]
    public async Task ListConversations_AfterReading_MarkedAsRead()
    {
        await StoreFor(Bob).SendMessage(Alice, "A message", CancellationToken.None);
        await StoreFor(Alice).ReadAll(Bob, CancellationToken.None).ToListAsync();

        var conversations = await StoreFor(Alice).ListConversations(CancellationToken.None).ToListAsync();

        Assert.That(conversations[0].Unread, Is.False);
    }

    // ── UpdateRead ─────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateRead_NoMessages_DoesNotInsertReadEvent()
    {
        await StoreFor(Alice).UpdateRead(Bob, CancellationToken.None);

        var rows = await CountRows();
        Assert.That(rows, Is.EqualTo(0));
    }

    [Test]
    public async Task UpdateRead_WithUnreadMessages_InsertsReadEvent()
    {
        await StoreFor(Bob).SendMessage(Alice, "Hey", CancellationToken.None);

        await StoreFor(Alice).UpdateRead(Bob, CancellationToken.None);

        // 1 message + 1 read event
        var rows = await CountRows();
        Assert.That(rows, Is.EqualTo(2));
    }

    [Test]
    public async Task UpdateRead_CalledTwice_DoesNotInsertDuplicateReadEvent()
    {
        await StoreFor(Bob).SendMessage(Alice, "Hey", CancellationToken.None);
        await StoreFor(Alice).UpdateRead(Bob, CancellationToken.None);

        // No new messages since the read → second UpdateRead should be a no-op
        await StoreFor(Alice).UpdateRead(Bob, CancellationToken.None);

        var rows = await CountRows();
        Assert.That(rows, Is.EqualTo(2)); // still 1 message + 1 read
    }

    // ── LatestChat ─────────────────────────────────────────────────────────

    [Test]
    public async Task LatestChat_NoMessages_ReturnsNull()
    {
        var latest = await StoreFor(Alice).LatestChat(CancellationToken.None);
        Assert.That(latest, Is.Null);
    }

    [Test]
    public async Task LatestChat_OneConversation_ReturnsThatPartner()
    {
        await StoreFor(Alice).SendMessage(Bob, "Hi", CancellationToken.None);

        var latest = await StoreFor(Alice).LatestChat(CancellationToken.None);

        Assert.That(latest, Is.EqualTo(Bob));
    }

    [Test]
    public async Task LatestChat_TwoConversations_ReturnsMostRecentPartner()
    {
        await StoreFor(Alice).SendMessage(Bob, "Hi Bob", CancellationToken.None);
        await StoreFor(Alice).SendMessage(Carol, "Hi Carol", CancellationToken.None);

        var latest = await StoreFor(Alice).LatestChat(CancellationToken.None);

        Assert.That(latest, Is.EqualTo(Carol));
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    ChatStore StoreFor(Username username)
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, username.Value)],
            authenticationType: "test"
        ));
        return new ChatStore(_conn, ctx);
    }

    async Task<int> CountRows()
    {
        await using var cmd = new NpgsqlCommand("SELECT COUNT(*)::int FROM stikl.chat_event", _conn);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }
}

// Convenience extension to materialise async enumerables in tests
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
