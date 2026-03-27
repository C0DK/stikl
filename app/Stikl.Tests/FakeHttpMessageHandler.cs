using System.Net;
using System.Text;

namespace Stikl.Tests;

/// <summary>
/// A test double for HttpMessageHandler that matches requests by URL predicate
/// and returns pre-configured JSON responses. Unmatched requests return 404.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(Func<string, bool> Match, string Json)> _rules = [];

    public int CallCount { get; private set; }

    public FakeHttpMessageHandler Add(Func<string, bool> match, string json)
    {
        _rules.Add((match, json));
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        CallCount++;
        var url = request.RequestUri?.ToString() ?? "";
        foreach (var (match, json) in _rules)
        {
            if (match(url))
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json"),
                    }
                );
        }
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
