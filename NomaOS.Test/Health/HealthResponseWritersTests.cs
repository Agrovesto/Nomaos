using System.Text.Json;
using Agrovesto.Backend.API.Health;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NomaOS.Test.Health;

/// <summary>
/// The writers are a critical path: a load balancer parses this body, and a
/// serialization change that drops a field silently breaks a deployment gate.
/// Every writer is asserted on the actual bytes written to the response.
/// </summary>
public class HealthResponseWritersTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 9, 25, 12, 30, 45, 123, TimeSpan.Zero);

    private static readonly BuildInfo DiBuildInfo = new(
        "nomaos-onboarding-api", "9.9.9", "fromdi1", "Production", "ip-10-0-1-42",
        new DateTimeOffset(2026, 9, 25, 10, 0, 0, TimeSpan.Zero));

    private readonly Func<DateTimeOffset> _originalClock = HealthResponseWriters.Clock;
    private readonly BuildInfo _originalFallback = HealthResponseWriters.FallbackBuildInfo;

    public HealthResponseWritersTests()
    {
        HealthResponseWriters.Clock = () => Now;
    }

    public void Dispose()
    {
        HealthResponseWriters.Clock = _originalClock;
        HealthResponseWriters.FallbackBuildInfo = _originalFallback;
        GC.SuppressFinalize(this);
    }

    private static HealthReport Report(HealthStatus status = HealthStatus.Healthy) =>
        new(
            new Dictionary<string, HealthReportEntry>(StringComparer.Ordinal)
            {
                ["self"] = new(HealthStatus.Healthy, "Process is running.", TimeSpan.FromMilliseconds(0.4), null, null, new[] { HealthTags.Live }),
                ["redis"] = new(status, "Redis responded.", TimeSpan.FromMilliseconds(2.5), null, null, new[] { HealthTags.Ready }),
            },
            TimeSpan.FromMilliseconds(3.1));

    private static DefaultHttpContext Context(IServiceProvider? services = null)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = services!,
        };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static JsonElement Body(HttpContext context)
    {
        var stream = (MemoryStream)context.Response.Body;
        stream.Position = 0;
        return JsonDocument.Parse(stream.ToArray()).RootElement;
    }

    [Fact]
    public async Task WriteLiveAsync_WritesTheMinimalLivenessBody()
    {
        var context = Context();

        await HealthResponseWriters.WriteLiveAsync(context, Report());

        context.Response.ContentType.Should().Be("application/json; charset=utf-8");
        context.Response.Headers["Cache-Control"].ToString().Should().Contain("no-store");

        var body = Body(context);
        body.GetProperty("status").GetString().Should().Be("Healthy");
        body.GetProperty("timestamp").GetString().Should().Be("2026-09-25T12:30:45.123Z");
        body.GetProperty("totalDurationMs").GetDouble().Should().Be(3.1);
        body.TryGetProperty("checks", out _).Should().BeFalse("liveness must not expose dependency detail");
    }

    [Fact]
    public async Task WriteReadyAsync_ListsTheDependencies()
    {
        var context = Context();

        await HealthResponseWriters.WriteReadyAsync(context, Report());

        var body = Body(context);
        body.GetProperty("status").GetString().Should().Be("Healthy");
        body.GetProperty("ready").GetBoolean().Should().BeTrue();
        body.GetProperty("checks").EnumerateArray().Select(check => check.GetProperty("name").GetString())
            .Should().Equal("redis", "self");
    }

    [Fact]
    public async Task WriteReadyAsync_ReportsNotReadyWhenADependencyIsDown()
    {
        var context = Context();

        await HealthResponseWriters.WriteReadyAsync(context, Report(HealthStatus.Unhealthy));

        Body(context).GetProperty("ready").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task WriteLegacyAsync_UsesTheBuildInfoFromDependencyInjection()
    {
        var services = new ServiceCollection().AddSingleton(DiBuildInfo).BuildServiceProvider();
        var context = Context(services);

        await HealthResponseWriters.WriteLegacyAsync(context, Report());

        var body = Body(context);
        body.GetProperty("version").GetString().Should().Be("9.9.9");
        body.GetProperty("application").GetString().Should().Be("nomaos-onboarding-api");
        body.GetProperty("uptime").GetString().Should().Be("0d 02h 30m 45s");
    }

    [Fact]
    public async Task WriteStatusAsync_IsTheOnCallView()
    {
        var services = new ServiceCollection().AddSingleton(DiBuildInfo).BuildServiceProvider();
        var context = Context(services);

        await HealthResponseWriters.WriteStatusAsync(context, Report(HealthStatus.Degraded));

        var body = Body(context);
        body.GetProperty("status").GetString().Should().Be("Degraded");
        body.GetProperty("live").GetBoolean().Should().BeTrue();
        body.GetProperty("ready").GetBoolean().Should().BeTrue("degraded dependencies still serve traffic");
        body.GetProperty("build").GetProperty("gitSha").GetString().Should().Be("fromdi1");
        body.GetProperty("build").GetProperty("environment").GetString().Should().Be("Production");
        body.GetProperty("build").GetProperty("uptimeSeconds").GetDouble().Should().Be(9045.123);
        body.GetProperty("memory").GetProperty("workingSetMb").GetDouble().Should().BeGreaterThan(0);
        body.GetProperty("summary").GetProperty("total").GetInt32().Should().Be(2);
        body.GetProperty("checks").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Writers_FallBackToTheStaticBuildInfoWhenDiHasNone()
    {
        HealthResponseWriters.FallbackBuildInfo = new BuildInfo(
            "fallback-app", "0.0.1", "fallbck", "Staging", "localhost", Now.AddMinutes(-1));

        var context = Context();

        await HealthResponseWriters.WriteLegacyAsync(context, Report());

        Body(context).GetProperty("version").GetString().Should().Be("0.0.1");
    }

    [Fact]
    public void ResolveBuildInfo_PrefersDependencyInjection()
    {
        var services = new ServiceCollection().AddSingleton(DiBuildInfo).BuildServiceProvider();

        HealthResponseWriters.ResolveBuildInfo(Context(services)).Should().BeSameAs(DiBuildInfo);
        HealthResponseWriters.ResolveBuildInfo(Context()).Should().BeSameAs(HealthResponseWriters.FallbackBuildInfo);
    }

    [Fact]
    public async Task Writers_RejectANullContext()
    {
        var live = async () => await HealthResponseWriters.WriteLiveAsync(null!, Report());
        var ready = async () => await HealthResponseWriters.WriteReadyAsync(null!, Report());

        await live.Should().ThrowAsync<ArgumentNullException>();
        await ready.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void SerializerOptions_AreCamelCaseAndShared()
    {
        HealthResponseWriters.SerializerOptions.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
        HealthResponseWriters.SerializerOptions.WriteIndented.Should().BeFalse();
    }
}
