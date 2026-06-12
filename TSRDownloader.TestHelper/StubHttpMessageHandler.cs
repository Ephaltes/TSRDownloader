using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TSRDownloader.TestHelper;

/// <summary>An HttpMessageHandler that maps requests to canned responses by predicate.</summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(Func<HttpRequestMessage, bool> Match, Func<HttpRequestMessage, HttpResponseMessage> Respond)> _rules = new();

    public List<HttpRequestMessage> Requests { get; } = new();

    public StubHttpMessageHandler When(
        Func<HttpRequestMessage, bool> match, Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _rules.Add((match, respond));
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        foreach ((Func<HttpRequestMessage, bool> match, Func<HttpRequestMessage, HttpResponseMessage> respond) in _rules)
        {
            if (match(request))
                return Task.FromResult(respond(request));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
