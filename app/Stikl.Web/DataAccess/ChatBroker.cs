using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Npgsql;
using Stikl.Web.Model;
using Stikl.Web.Routes;

namespace Stikl.Web.DataAccess;

public class ChatBroker(
    ILogger logger,
    NpgsqlDataSource db,
    IHttpContextAccessor HttpContextAccessor
) : BackgroundService
{
    private uint _idCursor;
    private readonly ConcurrentDictionary<uint, ChatSubscription> _subscriptions = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var conn = await db.OpenConnectionAsync(stoppingToken);
        conn.Notification += (o, e) =>
        {
            try
            {
                var entry = JsonSerializer.Deserialize<ChatEvent>(e.Payload)!;
                foreach (var (key, subscription) in _subscriptions)
                    subscription.Write(entry);
            }
            catch (Exception ex)
            {
                logger.ForContext("payload", e.Payload).Error(ex, "Could not broadcast update");
            }
        };

        await new NpgsqlCommand("LISTEN chat_messages", conn).ExecuteNonQueryAsync(stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
                await conn.WaitAsync(stoppingToken);
        }
        finally
        {
            foreach (var (id, subscription) in _subscriptions)
                await subscription.DisposeAsync();
            _subscriptions.Clear();
        }
    }

    public IAsyncEnumerator<ChatEvent> Subscribe(CancellationToken cancellationToken)
    {
        var user = HttpContextAccessor.HttpContext!.User.GetUsername();

        var id = Interlocked.Increment(ref _idCursor);
        var subscription = new ChatSubscription(
            () => RemoveSubscription(id),
            Channel.CreateUnbounded<ChatEvent>(
                new UnboundedChannelOptions() { SingleWriter = true, SingleReader = true }
            ),
            user,
            cancellationToken
        );
        if (!_subscriptions.TryAdd(id, subscription))
            throw new InvalidOperationException("Key already exist??");

        return subscription;
    }

    private void RemoveSubscription(uint id)
    {
        while (_subscriptions.ContainsKey(id) && !_subscriptions.TryRemove(id, out _))
            ;
    }

    private class ChatSubscription(
        Action disposeCallback,
        Channel<ChatEvent> channel,
        Username user,
        CancellationToken cancellationToken
    ) : IAsyncEnumerator<ChatEvent>
    {
        private IAsyncEnumerator<ChatEvent> _enumerator = channel
            .Reader.ReadAllAsync(cancellationToken)
            .GetAsyncEnumerator();
        public ChatEvent Current => _enumerator.Current;

        public void Write(ChatEvent entry)
        {
            if (entry.Recipient != user && entry.Sender != user)
                return;
            while (!channel.Writer.TryWrite(entry))
                ;
        }

        public async ValueTask DisposeAsync()
        {
            channel.Writer.TryComplete();
            disposeCallback();
            await _enumerator.DisposeAsync();
        }

        public ValueTask<bool> MoveNextAsync() => _enumerator.MoveNextAsync();
    }
}
