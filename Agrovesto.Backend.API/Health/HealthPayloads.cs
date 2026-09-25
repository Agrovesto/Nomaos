using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Agrovesto.Backend.API.Health;

/// <summary>One health check, flattened for JSON.</summary>
public sealed record HealthCheckPayload(
    string Name,
    string Status,
    double DurationMs,
    string? Description,
    IReadOnlyList<string> Tags,
    string? Error,
    IReadOnlyDictionary<string, string>? Data);

/// <summary>Body of <c>/health/live</c> — intentionally tiny.</summary>
public sealed record LivePayload(string Status, string Timestamp, double TotalDurationMs);

/// <summary>Body of <c>/health/ready</c>.</summary>
public sealed record ReadyPayload(
    string Status,
    bool Ready,
    string Timestamp,
    double TotalDurationMs,
    IReadOnlyList<HealthCheckPayload> Checks);

/// <summary>Body of the legacy <c>/health</c> summary.</summary>
public sealed record LegacyPayload(
    string Status,
    string Timestamp,
    string Application,
    string Version,
    string Uptime,
    double TotalDurationMs,
    IReadOnlyList<HealthCheckPayload> Checks);

/// <summary>Build/runtime identity block inside <c>/status</c>.</summary>
public sealed record StatusBuildPayload(
    string Application,
    string Version,
    string GitSha,
    string Environment,
    string MachineName,
    string StartedAtUtc,
    double UptimeSeconds,
    string Uptime);

/// <summary>Memory block inside <c>/status</c>.</summary>
public sealed record StatusMemoryPayload(
    double WorkingSetMb,
    double GcHeapMb,
    int ThreadCount);

/// <summary>Body of <c>/status</c> — the on-call view.</summary>
public sealed record StatusPayload(
    string Status,
    bool Live,
    bool Ready,
    string Timestamp,
    double TotalDurationMs,
    StatusBuildPayload Build,
    StatusMemoryPayload Memory,
    IReadOnlyDictionary<string, int> Summary,
    IReadOnlyList<HealthCheckPayload> Checks);

/// <summary>
/// Turns a <see cref="HealthReport"/> into the payload records above.
///
/// Kept separate from the writers so the shaping rules — which status means
/// "ready", how a degraded dependency is reported, what gets redacted — are
/// unit-testable without an HttpContext.
/// </summary>
public static class HealthPayloadFactory
{
    /// <summary>
    /// The product rule: Healthy and Degraded both mean "this instance can
    /// serve traffic". Only Unhealthy pulls the instance out of the pool.
    /// </summary>
    public static bool IsReady(HealthStatus status) => status != HealthStatus.Unhealthy;

    public static HealthCheckPayload ToPayload(string name, HealthReportEntry entry) =>
        new(
            Name: name,
            Status: entry.Status.ToString(),
            DurationMs: Math.Round(entry.Duration.TotalMilliseconds, 2),
            Description: entry.Description,
            Tags: entry.Tags?.ToArray() ?? Array.Empty<string>(),
            // Message only — a stack trace on a public probe endpoint is an
            // information-disclosure finding waiting to happen.
            Error: entry.Exception?.Message,
            Data: FlattenData(entry.Data));

    public static IReadOnlyList<HealthCheckPayload> ToPayloads(HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return report.Entries
            .Select(entry => ToPayload(entry.Key, entry.Value))
            .OrderBy(payload => payload.Name, StringComparer.Ordinal)
            .ToArray();
    }

    public static LivePayload CreateLive(HealthReport report, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new LivePayload(
            Status: report.Status.ToString(),
            Timestamp: FormatTimestamp(now),
            TotalDurationMs: Math.Round(report.TotalDuration.TotalMilliseconds, 2));
    }

    public static ReadyPayload CreateReady(HealthReport report, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new ReadyPayload(
            Status: report.Status.ToString(),
            Ready: IsReady(report.Status),
            Timestamp: FormatTimestamp(now),
            TotalDurationMs: Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            Checks: ToPayloads(report));
    }

    public static LegacyPayload CreateLegacy(HealthReport report, BuildInfo buildInfo, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(buildInfo);

        return new LegacyPayload(
            Status: report.Status.ToString(),
            Timestamp: FormatTimestamp(now),
            Application: buildInfo.Application,
            Version: buildInfo.Version,
            Uptime: buildInfo.UptimeHuman(now),
            TotalDurationMs: Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            Checks: ToPayloads(report));
    }

    public static StatusPayload CreateStatus(
        HealthReport report,
        BuildInfo buildInfo,
        MemorySnapshot memory,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(buildInfo);
        ArgumentNullException.ThrowIfNull(memory);

        var checks = ToPayloads(report);

        var summary = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["total"] = checks.Count,
            ["healthy"] = checks.Count(check => string.Equals(check.Status, nameof(HealthStatus.Healthy), StringComparison.Ordinal)),
            ["degraded"] = checks.Count(check => string.Equals(check.Status, nameof(HealthStatus.Degraded), StringComparison.Ordinal)),
            ["unhealthy"] = checks.Count(check => string.Equals(check.Status, nameof(HealthStatus.Unhealthy), StringComparison.Ordinal)),
        };

        return new StatusPayload(
            Status: report.Status.ToString(),
            // The process answered this request, so it is live by definition.
            Live: true,
            Ready: IsReadyFromEntries(report),
            Timestamp: FormatTimestamp(now),
            TotalDurationMs: Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            Build: new StatusBuildPayload(
                Application: buildInfo.Application,
                Version: buildInfo.Version,
                GitSha: buildInfo.GitSha,
                Environment: buildInfo.Environment,
                MachineName: buildInfo.MachineName,
                StartedAtUtc: FormatTimestamp(buildInfo.StartedAtUtc),
                UptimeSeconds: buildInfo.UptimeSeconds(now),
                Uptime: buildInfo.UptimeHuman(now)),
            Memory: new StatusMemoryPayload(memory.WorkingSetMb, memory.GcHeapMb, memory.ThreadCount),
            Summary: summary,
            Checks: checks);
    }

    /// <summary>
    /// Readiness as reported by /status considers only the checks tagged
    /// "ready" — a flaky third party (tagged "status") must not make the ops
    /// view claim the instance should be pulled from the load balancer.
    /// </summary>
    internal static bool IsReadyFromEntries(HealthReport report)
    {
        var readyEntries = report.Entries
            .Where(entry => entry.Value.Tags is not null && entry.Value.Tags.Contains(HealthTags.Ready))
            .ToArray();

        if (readyEntries.Length == 0)
        {
            return IsReady(report.Status);
        }

        return readyEntries.All(entry => IsReady(entry.Value.Status));
    }

    internal static IReadOnlyDictionary<string, string>? FlattenData(IReadOnlyDictionary<string, object>? data)
    {
        if (data is null || data.Count == 0)
        {
            return null;
        }

        var flattened = new Dictionary<string, string>(data.Count, StringComparer.Ordinal);

        foreach (var pair in data)
        {
            flattened[pair.Key] = pair.Value switch
            {
                null => string.Empty,
                IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
                _ => pair.Value.ToString() ?? string.Empty,
            };
        }

        return flattened;
    }

    internal static string FormatTimestamp(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture);
}
