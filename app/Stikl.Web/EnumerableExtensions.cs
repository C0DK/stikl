using System.Runtime.CompilerServices;

public static class EnumerableExtensions
{
    public static async IAsyncEnumerable<IReadOnlyList<T>> Batch<T>(
        this IAsyncEnumerable<T> source,
        int batchSize,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var batch = new List<T>(batchSize);

        await foreach (var item in source.WithCancellation(ct))
        {
            batch.Add(item);
            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>();
            }
        }
        if (batch.Count > 0)
            yield return batch;
    }
}
