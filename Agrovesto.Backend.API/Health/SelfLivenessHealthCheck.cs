using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Agrovesto.Backend.API.Health;

/// <summary>
/// The only check behind <c>/health/live</c>.
///
/// It answers exactly one question — "is this process still able to run managed
/// code and serve a request?" — and it answers it without touching SQL, Redis,
/// Vault or the network. That is the whole point: Docker's HEALTHCHECK and the
/// Compose healthcheck restart the container when liveness fails, so anything
/// that can fail for an external reason must NOT be in here. A database
/// failover would otherwise restart every container in the fleet simultaneously
/// and turn a 30 second blip into a cold-start outage.
/// </summary>
public sealed class SelfLivenessHealthCheck : IHealthCheck
{
    public const string Name = "self";

    private readonly BuildInfo _buildInfo;
    private readonly Func<DateTimeOffset> _clock;

    public SelfLivenessHealthCheck(BuildInfo buildInfo)
        : this(buildInfo, static () => DateTimeOffset.UtcNow)
    {
    }

    internal SelfLivenessHealthCheck(BuildInfo buildInfo, Func<DateTimeOffset> clock)
    {
        _buildInfo = buildInfo ?? throw new ArgumentNullException(nameof(buildInfo));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var now = _clock();

        var data = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["uptimeSeconds"] = _buildInfo.UptimeSeconds(now),
            ["version"] = _buildInfo.Version,
            ["gitSha"] = _buildInfo.GitSha,
        };

        return Task.FromResult(HealthCheckResult.Healthy("Process is running.", data));
    }
}
