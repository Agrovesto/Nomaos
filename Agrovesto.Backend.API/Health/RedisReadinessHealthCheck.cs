using System.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Agrovesto.Backend.API.Health;

/// <summary>
/// Readiness probe for Redis (ElastiCache in AWS, the redis container in the
/// dev profile).
///
/// Implemented on top of <see cref="IDistributedCache"/> — the exact abstraction
/// the application uses — rather than opening a second
/// <c>ConnectionMultiplexer</c>. That means the probe exercises the same
/// connection pool the request path uses, so a half-open or auth-failing
/// connection is actually detected, and it adds no additional TCP connections
/// per probe interval.
///
/// The cache is resolved lazily from the service provider so the check can be
/// registered unconditionally: an app wired without Redis reports Degraded
/// ("not configured") instead of throwing at startup.
/// </summary>
public sealed class RedisReadinessHealthCheck : IHealthCheck
{
    public const string Name = "redis";
    internal const string ProbeKey = "nomaos:health:probe";

    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public RedisReadinessHealthCheck(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration["Redis:ConnectionString"];
        var cache = _serviceProvider.GetService(typeof(IDistributedCache)) as IDistributedCache;

        if (cache is null)
        {
            return HealthCheckResult.Degraded("No IDistributedCache is registered; Redis is not in use.");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Degraded("Redis:ConnectionString is not configured.");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // A GET on a key that will never exist: no write amplification, no
            // TTL churn, and it still round-trips through the real connection.
            await cache.GetAsync(ProbeKey, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            return HealthCheckResult.Healthy(
                "Redis responded.",
                new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
                    ["endpoint"] = Redact(connectionString),
                });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The probe itself was cancelled (shutdown / per-check timeout).
            // Surfacing that as Unhealthy would flap readiness during a drain.
            stopwatch.Stop();
            return HealthCheckResult.Degraded("Redis probe cancelled.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return HealthCheckResult.Unhealthy(
                "Redis is not reachable.",
                ex,
                new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
                    ["endpoint"] = Redact(connectionString),
                });
        }
    }

    /// <summary>
    /// Strips everything after the host:port pair. Redis connection strings
    /// routinely carry "password=..." and this value ends up in /status, which
    /// dashboards and on-call screenshots love to leak.
    /// </summary>
    internal static string Redact(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "unset";
        }

        var firstSegment = connectionString.Split(',')[0].Trim();
        return string.IsNullOrEmpty(firstSegment) ? "unset" : firstSegment;
    }
}
