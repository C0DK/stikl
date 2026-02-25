namespace Stikl.Web.Routes;

public abstract class ServerSentEventResult : IResult
{
    public abstract IAsyncEnumerator<string> GetUpdates(CancellationToken cancellationToken);

    public async Task ExecuteAsync(HttpContext context)
    {
        var response = context.Response;
        response.Headers.Append("Cache-Control", "no-cache");
        response.Headers.ContentType = "text/event-stream";

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            context
                .RequestServices.GetRequiredService<IHostApplicationLifetime>()
                .ApplicationStopping,
            context.RequestAborted
        );

        await using var enumerator = GetUpdates(cts.Token);
        var moveNext = enumerator.MoveNextAsync().AsTask();
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                await Task.WhenAny(moveNext, Task.Delay(TimeSpan.FromSeconds(30), cts.Token));
                if (!moveNext.IsCompleted)
                {
                    // TODO: check toasts etc and add those if applicable!
                    await WriteAsync(response, "heartbeat", "", cts.Token);
                    continue;
                }
                if (!await moveNext)
                    break;

                await WriteAsync(response, "message", enumerator.Current, cts.Token);
                moveNext = enumerator.MoveNextAsync().AsTask();
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            cts.Cancel();
            try
            {
                await moveNext;
            }
            catch (OperationCanceledException) { }
        }
    }

    private async ValueTask WriteAsync(
        HttpResponse response,
        string eventType,
        string payload,
        CancellationToken cancellationToken
    )
    {
        await response.WriteAsync(
            $"event: {eventType}\ndata: {payload.Trim().ReplaceLineEndings("\ndata: ")}\n\n",
            cancellationToken
        );
        await response.Body.FlushAsync(cancellationToken);
    }
}
