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
    private readonly ConcurrentDictionary<uint, ChannelWriter<ChatMessage>> _subscriptions = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var conn = await db.OpenConnectionAsync(stoppingToken);
        conn.Notification += (o, e) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<ChatMessage>(e.Payload)!;
                foreach (var (key, channel) in _subscriptions)
                {
                    while (!channel.TryWrite(message))
                        ;
                }
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
            foreach (var (id, channel) in _subscriptions)
                channel.TryComplete();
            _subscriptions.Clear();
        }
    }

    public IAsyncEnumerator<ChatMessage> Subscribe(
        Username other,
        CancellationToken cancellationToken
    )
    {
        var user = HttpContextAccessor.HttpContext!.User.GetUsername();

        var channel = Channel.CreateUnbounded<ChatMessage>(
            new UnboundedChannelOptions() { SingleWriter = true, SingleReader = true }
        );
        var id = Interlocked.Increment(ref _idCursor);
        if (!_subscriptions.TryAdd(id, channel))
            throw new InvalidOperationException("Key already exist??");

        return new ChatSubscription(
            () => RemoveSubscription(id),
            channel.Reader,
            user,
            other,
            cancellationToken
        );
    }

    private void RemoveSubscription(uint id)
    {
        while (_subscriptions.ContainsKey(id) && !_subscriptions.TryRemove(id, out _))
            ;
    }

    private class ChatSubscription(
        Action disposeCallback,
        ChannelReader<ChatMessage> reader,
        Username memberA,
        Username memberB,
        CancellationToken cancellationToken
    ) : IAsyncEnumerator<ChatMessage>
    {
        private IAsyncEnumerator<ChatMessage> _enumerator = reader
            .ReadAllAsync(cancellationToken)
            .Where(message =>
                (message.Sender == memberA && message.Recipient == memberB)
                || (message.Sender == memberB && message.Recipient == memberA)
            )
            .GetAsyncEnumerator();
        public ChatMessage Current => _enumerator.Current;

        public async ValueTask DisposeAsync()
        {
            disposeCallback();
            await _enumerator.DisposeAsync();
        }

        public ValueTask<bool> MoveNextAsync() => _enumerator.MoveNextAsync();
    }
}
