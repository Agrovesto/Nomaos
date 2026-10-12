using Agrovesto.Backend.API.Health;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace NomaOS.Test.Health;

public class BuildInfoTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    private static readonly DateTimeOffset Started = new(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_PrefersGitShaOverCommitShaOverTheStampedFile()
    {
        BuildInfo.Create(Config(("GIT_SHA", "from-env"), ("COMMIT_SHA", "from-arg")), "Production", _ => "from-file")
            .GitSha.Should().Be("from-env");

        BuildInfo.Create(Config(("COMMIT_SHA", "from-arg")), "Production", _ => "from-file")
            .GitSha.Should().Be("from-arg");

        BuildInfo.Create(Config(), "Production", _ => "from-file")
            .GitSha.Should().Be("from-file");
    }

    [Fact]
    public void Create_FallsBackToUnknownWhenNothingIsStamped()
    {
        var info = BuildInfo.Create(Config(), "Production", _ => null);

        info.GitSha.Should().Be("unknown");
        info.Application.Should().Be(BuildInfo.ApplicationName);
        info.Environment.Should().Be("Production");
        info.MachineName.Should().Be(Environment.MachineName);
        info.Version.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Create_ReadsTheCommitShaFileFromTheImage()
    {
        string? requested = null;

        BuildInfo.Create(Config(), "Production", path =>
        {
            requested = path;
            return "  deadbeef \n";
        });

        requested.Should().Be(BuildInfo.CommitShaFile);
    }

    [Fact]
    public void Create_UsesTheServiceNameWhenConfigured()
    {
        BuildInfo.Create(Config(("OTEL_SERVICE_NAME", "nomaos-onboarding-api")), "Staging", _ => null)
            .Application.Should().Be("nomaos-onboarding-api");
    }

    [Fact]
    public void Create_DefaultsAnEmptyEnvironmentNameToProduction()
    {
        BuildInfo.Create(Config(), "  ", _ => null).Environment.Should().Be("Production");
    }

    [Fact]
    public void Create_DefaultsToTheProcessStartTime()
    {
        var info = BuildInfo.Create(Config(), "Production", _ => null);

        info.StartedAtUtc.Should().Be(BuildInfo.ProcessStartedAtUtc);
        BuildInfo.ProcessStartedAtUtc.Should().BeBefore(DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Create_WithoutAFileReader_DoesNotThrow()
    {
        // The default reader hits the filesystem; /app/.commit-sha does not
        // exist on a dev box and that must be a silent miss, not an exception.
        var act = () => BuildInfo.Create(Config(), "Production");

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_RejectsNullConfiguration()
    {
        var act = () => BuildInfo.Create(null!, "Production");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UptimeSeconds_IsRoundedAndNeverNegative()
    {
        var info = new BuildInfo("app", "1.0.0", "sha", "Production", "host", Started);

        info.UptimeSeconds(Started.AddSeconds(90.1234)).Should().Be(90.123);
        info.UptimeSeconds(Started.AddSeconds(-30)).Should().Be(0);
    }

    [Fact]
    public void UptimeHuman_IsReadableOnACall()
    {
        var info = new BuildInfo("app", "1.0.0", "sha", "Production", "host", Started);

        info.UptimeHuman(Started.AddDays(3).AddHours(4).AddMinutes(7).AddSeconds(12))
            .Should().Be("3d 04h 07m 12s");

        info.UptimeHuman(Started.AddMinutes(-5)).Should().Be("0d 00h 00m 00s");
    }

    [Fact]
    public void ResolveVersion_NeverReturnsTheSourceLinkSuffix()
    {
        BuildInfo.ResolveVersion(typeof(BuildInfo).Assembly)
            .Should().NotBeNullOrWhiteSpace().And.NotContain("+");
    }

    [Fact]
    public void MemorySnapshot_ReportsTheCurrentProcess()
    {
        var snapshot = MemorySnapshot.Capture();

        snapshot.WorkingSetBytes.Should().BeGreaterThan(0);
        snapshot.GcHeapBytes.Should().BeGreaterThan(0);
        snapshot.ThreadCount.Should().BeGreaterThan(0);
        snapshot.WorkingSetMb.Should().BeGreaterThan(0);
        snapshot.GcHeapMb.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MemorySnapshot_ConvertsBytesToMegabytes()
    {
        var snapshot = new MemorySnapshot(WorkingSetBytes: 157_286_400, GcHeapBytes: 52_428_800, ThreadCount: 24);

        snapshot.WorkingSetMb.Should().Be(150);
        snapshot.GcHeapMb.Should().Be(50);
    }
}
