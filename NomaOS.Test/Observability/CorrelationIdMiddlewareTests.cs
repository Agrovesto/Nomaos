using System.Diagnostics;
using Agrovesto.Backend.API.Observability;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace NomaOS.Test.Observability;

/// <summary>
/// Pins the two halves of the correlation contract that matter operationally:
/// a caller can always read the id back off the response, and a caller can never
/// make the service write arbitrary text into its own logs and headers.
/// </summary>
public class CorrelationIdMiddlewareTests
{
    private static async Task<(DefaultHttpContext Context, string? SeenByHandler)> RunAsync(string? inbound)
    {
        var context = new DefaultHttpContext();

        if (inbound is not null)
        {
            context.Request.Headers.Append(CorrelationIdMiddleware.HeaderName, inbound);
        }

        string? seenByHandler = null;

        var middleware = new CorrelationIdMiddleware(_ =>
        {
            seenByHandler = context.GetCorrelationId();
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        return (context, seenByHandler);
    }

    private static string ResponseId(DefaultHttpContext context) =>
        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();

    [Fact]
    public async Task InvokeAsync_GeneratesAnOpaqueIdWhenTheClientSentNone()
    {
        var (context, seenByHandler) = await RunAsync(inbound: null);

        ResponseId(context).Should().MatchRegex("^[0-9a-f]{32}$");
        seenByHandler.Should().Be(ResponseId(context));
    }

    [Fact]
    public async Task InvokeAsync_HonoursAWellFormedInboundId()
    {
        var (context, seenByHandler) = await RunAsync("  corr_2026.10-14_A  ");

        ResponseId(context).Should().Be("corr_2026.10-14_A");
        seenByHandler.Should().Be("corr_2026.10-14_A");
    }

    [Theory]
    [InlineData("00000000-0000-4000-8000-000000000000")]
    [InlineData("nomaos-web-9f2c")]
    [InlineData("A1._-z9")]
    [InlineData("x")]
    public async Task InvokeAsync_EchoesAnythingInTheAllowList(string inbound)
    {
        var (context, _) = await RunAsync(inbound);

        ResponseId(context).Should().Be(inbound);
    }

    [Theory]
    // Header/log splitting and JSON-line breaking: these are the shapes that
    // would otherwise be copied verbatim into every Loki line for the request.
    [InlineData("abc\r\nX-Injected: 1")]
    [InlineData("abc def")]
    [InlineData("abc\"def")]
    [InlineData("abc{de}f")]
    [InlineData("abc;DROP TABLE")]
    // Non-ASCII round-trips through HTTP/2 headers but breaks Grafana links.
    [InlineData("bónú-id")]
    public async Task InvokeAsync_ReplacesAnythingItCannotTrust(string inbound)
    {
        var (context, seenByHandler) = await RunAsync(inbound);

        ResponseId(context).Should().MatchRegex("^[0-9a-f]{32}$");
        ResponseId(context).Should().NotBe(inbound);
        seenByHandler.Should().Be(ResponseId(context));
    }

    [Fact]
    public async Task InvokeAsync_ReplacesAnOversizedIdInsteadOfTruncatingIt()
    {
        // Truncating-and-keeping would hand two services different ids for the
        // same request, which is worse than generating a fresh one.
        var inbound = new string('a', CorrelationIdMiddleware.MaxAcceptedLength + 1);

        var (context, _) = await RunAsync(inbound);

        ResponseId(context).Length.Should().Be(CorrelationIdMiddleware.GeneratedLength);
        ResponseId(context).Should().NotBe(inbound[..CorrelationIdMiddleware.MaxAcceptedLength]);
    }

    [Fact]
    public async Task InvokeAsync_UsesGeneratedIdWhenInboundIsOnlyWhitespace()
    {
        var (context, _) = await RunAsync("   ");

        ResponseId(context).Should().MatchRegex("^[0-9a-f]{32}$");
    }

    [Fact]
    public async Task InvokeAsync_TakesTheFirstValueOfADuplicatedHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(CorrelationIdMiddleware.HeaderName, "first-value");
        context.Request.Headers.Append(CorrelationIdMiddleware.HeaderName, "second-value");

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);

        // Echoing "first-value,second-value" (what StringValues.ToString() does)
        // would put a comma and a space into the id; the caller would then be
        // unable to find its own request by the value it was given.
        ResponseId(context).Should().Be("first-value");
    }

    [Fact]
    public void GetCorrelationId_IsNullBeforeTheMiddlewareRuns()
    {
        new DefaultHttpContext().GetCorrelationId().Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_StampsTheCorrelationTagOnTheAmbientActivity()
    {
        // A sampled activity stands in for the one the AspNetCore instrumentation
        // creates before the pipeline runs. Without AllDataAndRecorded the SDK
        // drops tags, which is exactly the silent failure this guards against.
        var sourceName = "CorrelationIdMiddlewareTests." + Guid.NewGuid().ToString("N");

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == sourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);

        using var source = new ActivitySource(sourceName);
        using var activity = source.StartActivity("GET /api/v2/organizations");
        activity.Should().NotBeNull();

        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);

        activity!.GetTagItem("nomaos.correlation_id").Should().Be(ResponseId(context));
    }

    [Fact]
    public async Task InvokeAsync_StillIssuesAnIdWhenThereIsNoActivity()
    {
        // The OTEL_SDK_DISABLED=true escape hatch, and any non-web host: no
        // ambient activity exists, and that must not break request handling.
        var previous = Activity.Current;
        Activity.Current = null;

        try
        {
            var context = new DefaultHttpContext();
            var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

            var act = async () => await middleware.InvokeAsync(context);

            await act.Should().NotThrowAsync();
            ResponseId(context).Should().MatchRegex("^[0-9a-f]{32}$");
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("ok.id-1_2", true)]
    [InlineData("with space", false)]
    [InlineData("with/slash", false)]
    [InlineData("with;semicolon", false)]
    [InlineData("with\nnewline", false)]
    public void IsWellFormed_ImplementsTheAllowListExactly(string? value, bool expected)
    {
        CorrelationIdMiddleware.IsWellFormed(value).Should().Be(expected);
    }

    [Fact]
    public void IsWellFormed_BoundsAtExactlyMaxAcceptedLength()
    {
        var atLimit = new string('a', CorrelationIdMiddleware.MaxAcceptedLength);
        var overLimit = new string('a', CorrelationIdMiddleware.MaxAcceptedLength + 1);

        CorrelationIdMiddleware.IsWellFormed(atLimit).Should().BeTrue();
        CorrelationIdMiddleware.IsWellFormed(overLimit).Should().BeFalse();
    }

    [Fact]
    public void Constructor_RejectsANullNextDelegate()
    {
        var act = () => new CorrelationIdMiddleware(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
