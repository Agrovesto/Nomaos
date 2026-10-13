using Agrovesto.Backend.API.Health;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NomaOS.Test.Health;

public class SelfLivenessHealthCheckTests
{
    private static readonly DateTimeOffset Started = new(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);

    private static BuildInfo Build() =>
        new("nomaos-onboarding-api", "1.4.2", "abc1234", "Production", "ip-10-0-1-42", Started);

    private static HealthCheckContext Context() =>
        new()
        {
            Registration = new HealthCheckRegistration(
                SelfLivenessHealthCheck.Name,
                new SelfLivenessHealthCheck(Build()),
                HealthStatus.Unhealthy,
                new[] { HealthTags.Live }),
        };

    [Fact]
    public async Task CheckHealthAsync_IsAlwaysHealthyAndCarriesTheBuildIdentity()
    {
        var check = new SelfLivenessHealthCheck(Build(), () => Started.AddSeconds(125));

        var result = await check.CheckHealthAsync(Context(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Process is running.");
        result.Data["uptimeSeconds"].Should().Be(125d);
        result.Data["version"].Should().Be("1.4.2");
        result.Data["gitSha"].Should().Be("abc1234");
    }

    [Fact]
    public async Task CheckHealthAsync_DoesNotObserveCancellation()
    {
        // Liveness must answer even while the host is shutting down; Docker
        // restarts the container when it does not.
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await new SelfLivenessHealthCheck(Build()).CheckHealthAsync(Context(), cts.Token);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void Constructor_RejectsNullDependencies()
    {
        var nullBuildInfo = () => new SelfLivenessHealthCheck(null!);
        var nullClock = () => new SelfLivenessHealthCheck(Build(), null!);

        nullBuildInfo.Should().Throw<ArgumentNullException>();
        nullClock.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CheckHealthAsync_UsesTheWallClockByDefault()
    {
        var result = await new SelfLivenessHealthCheck(Build()).CheckHealthAsync(Context(), CancellationToken.None);

        ((double)result.Data["uptimeSeconds"]).Should().BeGreaterThan(0);
    }
}
