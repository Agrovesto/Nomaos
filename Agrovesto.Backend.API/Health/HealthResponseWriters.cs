using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Agrovesto.Backend.API.Health;

/// <summary>
/// JSON response writers for the four probe endpoints.
///
/// They serialize straight to <c>Response.Body</c> with a shared, cached
/// <see cref="JsonSerializerOptions"/> instance. That matters more than it
/// looks: these endpoints are hit several times a second by Docker, Compose
/// and the load balancer, and allocating a new JsonSerializerOptions per
/// request would rebuild the serializer metadata cache every time — a
/// well-known way to burn CPU on health checks.
///
/// Status codes are NOT set here; they come from
/// <c>HealthCheckOptions.ResultStatusCodes</c> in <see cref="HealthCheckExtensions"/>,
/// so the mapping stays in one place.
/// </summary>
public static class HealthResponseWriters
{
    public const string ContentType = "application/json; charset=utf-8";

    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    /// <summary>Overridable clock so the tests can assert on the timestamp.</summary>
    internal static Func<DateTimeOffset> Clock { get; set; } = static () => DateTimeOffset.UtcNow;

    /// <summary>Build identity used by /health and /status when DI has none registered.</summary>
    internal static BuildInfo FallbackBuildInfo { get; set; } = new(
        application: BuildInfo.ApplicationName,
        version: "0.0.0",
        gitSha: "unknown",
        environment: "Production",
        machineName: Environment.MachineName,
        startedAtUtc: BuildInfo.ProcessStartedAtUtc);

    public static Task WriteLiveAsync(HttpContext context, HealthReport report) =>
        WriteAsync(context, HealthPayloadFactory.CreateLive(report, Clock()));

    public static Task WriteReadyAsync(HttpContext context, HealthReport report) =>
        WriteAsync(context, HealthPayloadFactory.CreateReady(report, Clock()));

    public static Task WriteLegacyAsync(HttpContext context, HealthReport report) =>
        WriteAsync(context, HealthPayloadFactory.CreateLegacy(report, ResolveBuildInfo(context), Clock()));

    public static Task WriteStatusAsync(HttpContext context, HealthReport report) =>
        WriteAsync(
            context,
            HealthPayloadFactory.CreateStatus(report, ResolveBuildInfo(context), MemorySnapshot.Capture(), Clock()));

    internal static BuildInfo ResolveBuildInfo(HttpContext context) =>
        context.RequestServices?.GetService(typeof(BuildInfo)) as BuildInfo ?? FallbackBuildInfo;

    private static async Task WriteAsync<TPayload>(HttpContext context, TPayload payload)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.ContentType = ContentType;
        // Probes must never be served from a cache — a 200 pinned by an
        // intermediary would keep a dead instance in rotation.
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            payload,
            SerializerOptions,
            context.RequestAborted).ConfigureAwait(false);
    }
}
