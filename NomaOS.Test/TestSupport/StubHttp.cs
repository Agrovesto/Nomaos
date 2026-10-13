using System.Net;

namespace NomaOS.Test.TestSupport;

/// <summary>
/// Deterministic HttpClient plumbing for the probe tests.
///
/// The health suite must never touch the network: a test that quietly resolves
/// vault.agrovesto.com is a test that fails at 2am in CI for reasons unrelated
/// to the code under test. Everything goes through this handler.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(HttpStatusCode statusCode, string content = "{}")
        : this(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content),
        })
    {
    }

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder ?? throw new ArgumentNullException(nameof(responder));
    }

    public List<HttpRequestMessage> Requests { get; } = new();

    public static StubHttpMessageHandler Throwing(Exception exception) =>
        new(_ => throw exception);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_responder(request));
    }
}

/// <summary>Hands every named client the same stub handler.</summary>
public sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly StubHttpMessageHandler _handler;

    public StubHttpClientFactory(StubHttpMessageHandler handler)
    {
        _handler = handler;
    }

    public List<string> RequestedNames { get; } = new();

    public HttpClient CreateClient(string name)
    {
        RequestedNames.Add(name);
        return new HttpClient(_handler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromSeconds(5),
        };
    }
}
