using Agrovesto.Backend.API.Observability;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace NomaOS.Test.Observability;

public class ObservabilityPathsTests
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/")]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/health/detailed")]
    [InlineData("/HEALTH/LIVE")]
    [InlineData("/status")]
    [InlineData("/Status")]
    [InlineData("/hangfire")]
    [InlineData("/hangfire/jobs/enqueued")]
    public void IsExcluded_ExcludesProbeAndDashboardTraffic(string path)
    {
        ObservabilityPaths.IsExcluded(path).Should().BeTrue();
    }

    [Theory]
    [InlineData("/api/v2/user/ping")]
    [InlineData("/healthz")]
    [InlineData("/healthcheck/live")]
    [InlineData("/statuses")]
    [InlineData("/hangfired")]
    [InlineData("/swagger/index.html")]
    [InlineData("/")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsExcluded_KeepsRealTraffic(string? path)
    {
        ObservabilityPaths.IsExcluded(path).Should().BeFalse();
    }

    [Fact]
    public void IsExcluded_PathString_DelegatesToStringOverload()
    {
        ObservabilityPaths.IsExcluded(new PathString("/health/ready")).Should().BeTrue();
        ObservabilityPaths.IsExcluded(new PathString("/api/v2/farm/ping")).Should().BeFalse();
        ObservabilityPaths.IsExcluded(default(PathString)).Should().BeFalse();
    }

    [Fact]
    public void IsExcluded_Uri_UsesAbsolutePath()
    {
        ObservabilityPaths.IsExcluded(new Uri("http://127.0.0.1:8080/health/live")).Should().BeTrue();
        ObservabilityPaths.IsExcluded(new Uri("https://api.nomaos.com/api/v2/user/ping")).Should().BeFalse();
        ObservabilityPaths.IsExcluded((Uri?)null).Should().BeFalse();
    }

    [Fact]
    public void ExcludedPrefixes_CoverTheDocumentedSet()
    {
        ObservabilityPaths.ExcludedPrefixes.Should().BeEquivalentTo(new[] { "/health", "/status", "/hangfire" });
    }
}
