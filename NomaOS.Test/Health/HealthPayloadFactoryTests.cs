using Agrovesto.Backend.API.Health;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NomaOS.Test.Health;

public class HealthPayloadFactoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 25, 12, 30, 45, 123, TimeSpan.Zero);

    private static readonly BuildInfo Build = new(
        application: "nomaos-onboarding-api",
        version: "1.4.2",
        gitSha: "abc1234",
        environment: "Production",
        machineName: "ip-10-0-1-42",
        startedAtUtc: new DateTimeOffset(2026, 9, 25, 10, 0, 0, TimeSpan.Zero));

    private static HealthReportEntry Entry(
        HealthStatus status,
        string? description = null,
        double durationMs = 12.3456,
        Exception? exception = null,
        IReadOnlyDictionary<string, object>? data = null,
        string[]? tags = null) =>
        new(status, description, TimeSpan.FromMilliseconds(durationMs), exception, data, tags);

    private static HealthReport Report(params (string Name, HealthReportEntry Entry)[] entries) =>
        new(
            entries.ToDictionary(e => e.Name, e => e.Entry, StringComparer.Ordinal),
            TimeSpan.FromMilliseconds(42.5));

    [Theory]
    [InlineData(HealthStatus.Healthy, true)]
    [InlineData(HealthStatus.Degraded, true)]
    [InlineData(HealthStatus.Unhealthy, false)]
    public void IsReady_TreatsDegradedAsServable(HealthStatus status, bool expected)
    {
        HealthPayloadFactory.IsReady(status).Should().Be(expected);
    }

    [Fact]
    public void ToPayload_FlattensAnEntry()
    {
        var payload = HealthPayloadFactory.ToPayload(
            "redis",
            Entry(
                HealthStatus.Healthy,
                "Redis responded.",
                3.21,
                data: new Dictionary<string, object> { ["latencyMs"] = 3.21, ["endpoint"] = "redis:6379" },
                tags: new[] { "ready" }));

        payload.Name.Should().Be("redis");
        payload.Status.Should().Be("Healthy");
        payload.DurationMs.Should().Be(3.21);
        payload.Description.Should().Be("Redis responded.");
        payload.Tags.Should().Equal("ready");
        payload.Error.Should().BeNull();
        payload.Data!["endpoint"].Should().Be("redis:6379");
        payload.Data!["latencyMs"].Should().Be("3.21");
    }

    [Fact]
    public void ToPayload_ReportsTheExceptionMessageButNeverTheStackTrace()
    {
        Exception captured;
        try
        {
            throw new TimeoutException("SQL Server did not answer in 5s");
        }
        catch (TimeoutException ex)
        {
            captured = ex;
        }

        captured.StackTrace.Should().NotBeNullOrEmpty("the test needs a realistic exception");

        var payload = HealthPayloadFactory.ToPayload("sql", Entry(HealthStatus.Unhealthy, exception: captured));

        payload.Error.Should().Be("SQL Server did not answer in 5s");
        payload.Error.Should().NotContain("at NomaOS.Test");
    }

    [Fact]
    public void ToPayloads_AreSortedAndStable()
    {
        var report = Report(
            ("vault", Entry(HealthStatus.Healthy)),
            ("redis", Entry(HealthStatus.Healthy)),
            ("self", Entry(HealthStatus.Healthy)));

        HealthPayloadFactory.ToPayloads(report).Select(payload => payload.Name)
            .Should().Equal("redis", "self", "vault");
    }

    [Fact]
    public void CreateLive_IsMinimal()
    {
        var payload = HealthPayloadFactory.CreateLive(Report(("self", Entry(HealthStatus.Healthy))), Now);

        payload.Status.Should().Be("Healthy");
        payload.TotalDurationMs.Should().Be(42.5);
        payload.Timestamp.Should().Be("2026-09-25T12:30:45.123Z");
    }

    [Fact]
    public void CreateReady_MarksDegradedDependenciesAsStillServing()
    {
        var payload = HealthPayloadFactory.CreateReady(
            Report(
                ("redis", Entry(HealthStatus.Healthy, tags: new[] { "ready" })),
                ("vault", Entry(HealthStatus.Degraded, "VAULT_ADDR is not configured", tags: new[] { "ready" }))),
            Now);

        payload.Status.Should().Be("Degraded");
        payload.Ready.Should().BeTrue();
        payload.Checks.Should().HaveCount(2);
    }

    [Fact]
    public void CreateReady_MarksUnhealthyDependenciesAsNotServing()
    {
        var payload = HealthPayloadFactory.CreateReady(
            Report(("sql", Entry(HealthStatus.Unhealthy, "connection refused", tags: new[] { "ready" }))),
            Now);

        payload.Status.Should().Be("Unhealthy");
        payload.Ready.Should().BeFalse();
    }

    [Fact]
    public void CreateLegacy_KeepsTheBackCompatFields()
    {
        var payload = HealthPayloadFactory.CreateLegacy(
            Report(("self", Entry(HealthStatus.Healthy))),
            Build,
            Now);

        payload.Application.Should().Be("nomaos-onboarding-api");
        payload.Version.Should().Be("1.4.2");
        payload.Uptime.Should().Be("0d 02h 30m 45s");
        payload.Checks.Should().ContainSingle();
    }

    [Fact]
    public void CreateStatus_IsTheOnCallView()
    {
        var payload = HealthPayloadFactory.CreateStatus(
            Report(
                ("self", Entry(HealthStatus.Healthy, tags: new[] { "live" })),
                ("redis", Entry(HealthStatus.Healthy, tags: new[] { "ready" })),
                ("vault", Entry(HealthStatus.Degraded, tags: new[] { "ready" })),
                ("SendGrid API", Entry(HealthStatus.Unhealthy, tags: new[] { "status" }))),
            Build,
            new MemorySnapshot(157_286_400, 52_428_800, 24),
            Now);

        payload.Live.Should().BeTrue();
        // A dead third party (tagged "status") must not claim the instance
        // should leave the load balancer.
        payload.Ready.Should().BeTrue();
        payload.Build.GitSha.Should().Be("abc1234");
        payload.Build.Environment.Should().Be("Production");
        payload.Build.MachineName.Should().Be("ip-10-0-1-42");
        payload.Build.UptimeSeconds.Should().Be(9045.123);
        payload.Build.StartedAtUtc.Should().Be("2026-09-25T10:00:00.000Z");
        payload.Memory.WorkingSetMb.Should().Be(150);
        payload.Memory.ThreadCount.Should().Be(24);
        payload.Summary["total"].Should().Be(4);
        payload.Summary["healthy"].Should().Be(2);
        payload.Summary["degraded"].Should().Be(1);
        payload.Summary["unhealthy"].Should().Be(1);
        payload.Checks.Should().HaveCount(4);
    }

    [Fact]
    public void CreateStatus_ReportsNotReadyWhenAReadyTaggedCheckIsDown()
    {
        var payload = HealthPayloadFactory.CreateStatus(
            Report(
                ("self", Entry(HealthStatus.Healthy, tags: new[] { "live" })),
                ("sql", Entry(HealthStatus.Unhealthy, tags: new[] { "ready" }))),
            Build,
            new MemorySnapshot(1, 1, 1),
            Now);

        payload.Ready.Should().BeFalse();
        payload.Live.Should().BeTrue("the process answered the request");
    }

    [Fact]
    public void IsReadyFromEntries_FallsBackToTheOverallStatusWithoutReadyTags()
    {
        HealthPayloadFactory
            .IsReadyFromEntries(Report(("self", Entry(HealthStatus.Healthy, tags: new[] { "live" }))))
            .Should().BeTrue();

        HealthPayloadFactory
            .IsReadyFromEntries(Report(("boom", Entry(HealthStatus.Unhealthy, tags: new[] { "live" }))))
            .Should().BeFalse();
    }

    [Fact]
    public void FlattenData_StringifiesValuesInvariantlyAndDropsEmptyDictionaries()
    {
        HealthPayloadFactory.FlattenData(null).Should().BeNull();
        HealthPayloadFactory.FlattenData(new Dictionary<string, object>()).Should().BeNull();

        var flattened = HealthPayloadFactory.FlattenData(new Dictionary<string, object>
        {
            ["latencyMs"] = 12.5,
            ["statusCode"] = 503,
            ["enabled"] = true,
            ["endpoint"] = "redis:6379",
            ["nothing"] = null!,
        })!;

        flattened["latencyMs"].Should().Be("12.5");
        flattened["statusCode"].Should().Be("503");
        flattened["enabled"].Should().Be("True");
        flattened["endpoint"].Should().Be("redis:6379");
        flattened["nothing"].Should().BeEmpty();
    }

    [Fact]
    public void FormatTimestamp_IsAlwaysUtcIso8601()
    {
        HealthPayloadFactory
            .FormatTimestamp(new DateTimeOffset(2026, 9, 25, 15, 30, 45, 123, TimeSpan.FromHours(1)))
            .Should().Be("2026-09-25T14:30:45.123Z");
    }

    [Fact]
    public void Factories_RejectNullArguments()
    {
        var report = Report(("self", Entry(HealthStatus.Healthy)));
        var memory = new MemorySnapshot(1, 1, 1);

        ((Action)(() => HealthPayloadFactory.ToPayloads(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => HealthPayloadFactory.CreateLive(null!, Now))).Should().Throw<ArgumentNullException>();
        ((Action)(() => HealthPayloadFactory.CreateReady(null!, Now))).Should().Throw<ArgumentNullException>();
        ((Action)(() => HealthPayloadFactory.CreateLegacy(null!, Build, Now))).Should().Throw<ArgumentNullException>();
        ((Action)(() => HealthPayloadFactory.CreateLegacy(report, null!, Now))).Should().Throw<ArgumentNullException>();
        ((Action)(() => HealthPayloadFactory.CreateStatus(null!, Build, memory, Now))).Should().Throw<ArgumentNullException>();
        ((Action)(() => HealthPayloadFactory.CreateStatus(report, null!, memory, Now))).Should().Throw<ArgumentNullException>();
        ((Action)(() => HealthPayloadFactory.CreateStatus(report, Build, null!, Now))).Should().Throw<ArgumentNullException>();
    }
}
