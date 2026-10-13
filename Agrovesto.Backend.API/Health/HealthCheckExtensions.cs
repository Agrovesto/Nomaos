using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Agrovesto.Backend.API.Health;

/// <summary>
/// Registration + endpoint mapping for the probe surface.
///
/// | Endpoint       | Checks                    | 200                  | 503        | Consumer                    |
/// |----------------|---------------------------|----------------------|------------|-----------------------------|
/// | /health/live   | tag "live" (self only)    | Healthy/Degraded     | Unhealthy  | Docker HEALTHCHECK, Compose |
/// | /health/ready  | tag "ready" (SQL/Redis/Vault) | Healthy/Degraded | Unhealthy  | nginx / ALB / depends_on    |
/// | /health        | live + ready              | Healthy/Degraded     | Unhealthy  | back-compat                 |
/// | /status        | everything                | always 200           | never      | dashboards / on-call        |
///
/// /status always returns 200 on purpose: it is a *report*, not a gate. A
/// dashboard that cannot fetch the body because the endpoint 503s is a
/// dashboard that tells you nothing during the incident.
/// </summary>
public static class HealthCheckExtensions
{
    private static readonly TimeSpan DependencyTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Singletons and the named HttpClient the probes need. Safe to call more
    /// than once.
    /// </summary>
    public static IServiceCollection AddNomaosHealthInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<BuildInfo>(static provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var environmentName = (provider.GetService(typeof(IHostEnvironment)) as IHostEnvironment)?.EnvironmentName
                                  ?? "Production";
            return BuildInfo.Create(configuration, environmentName);
        });

        // Bounded timeout: the default HttpClient timeout is 100s, which would
        // let a hung Vault stall a readiness probe long past the point where
        // the load balancer has already given up.
        services.AddHttpClient(VaultReadinessHealthCheck.HttpClientName, static client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("User-Agent", "Agrovesto-HealthCheck/1.0");
        });

        return services;
    }

    /// <summary>
    /// Adds the liveness self-check plus the Redis and Vault readiness checks.
    /// SQL Server is registered by the caller (it already owns the connection
    /// string and the EF DbContext check) and must carry the "ready" tag.
    /// </summary>
    public static IHealthChecksBuilder AddNomaosProbeChecks(this IHealthChecksBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddNomaosHealthInfrastructure();

        builder.AddCheck<SelfLivenessHealthCheck>(
            SelfLivenessHealthCheck.Name,
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { HealthTags.Live });

        builder.AddCheck<RedisReadinessHealthCheck>(
            RedisReadinessHealthCheck.Name,
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { HealthTags.Ready },
            timeout: DependencyTimeout);

        builder.AddCheck<VaultReadinessHealthCheck>(
            VaultReadinessHealthCheck.Name,
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { HealthTags.Ready },
            timeout: DependencyTimeout);

        return builder;
    }

    public static IEndpointRouteBuilder MapNomaosHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapHealthChecks(
            HealthEndpoints.Live,
            CreateOptions(
                static registration => registration.Tags.Contains(HealthTags.Live),
                HealthResponseWriters.WriteLiveAsync,
                gating: true)).AllowAnonymous();

        endpoints.MapHealthChecks(
            HealthEndpoints.Ready,
            CreateOptions(
                static registration => registration.Tags.Contains(HealthTags.Ready),
                HealthResponseWriters.WriteReadyAsync,
                gating: true)).AllowAnonymous();

        endpoints.MapHealthChecks(
            HealthEndpoints.Legacy,
            CreateOptions(
                static registration =>
                    registration.Tags.Contains(HealthTags.Live) || registration.Tags.Contains(HealthTags.Ready),
                HealthResponseWriters.WriteLegacyAsync,
                gating: true)).AllowAnonymous();

        endpoints.MapHealthChecks(
            HealthEndpoints.Status,
            CreateOptions(
                static _ => true,
                HealthResponseWriters.WriteStatusAsync,
                gating: false)).AllowAnonymous();

        return endpoints;
    }

    /// <summary>
    /// Builds the endpoint options.
    ///
    /// Degraded is a 200 on the gating endpoints: a degraded dependency (Vault
    /// answering slowly, SendGrid down) still serves customer traffic
    /// correctly. Only Unhealthy removes the instance. When
    /// <paramref name="gating"/> is false every status maps to 200 — that is
    /// /status, which reports rather than gates.
    ///
    /// ResultStatusCodes is mutated rather than assigned because the property
    /// is populated with defaults by the framework.
    /// </summary>
    internal static HealthCheckOptions CreateOptions(
        Func<HealthCheckRegistration, bool> predicate,
        Func<HttpContext, HealthReport, Task> responseWriter,
        bool gating)
    {
        var options = new HealthCheckOptions
        {
            Predicate = predicate,
            ResponseWriter = responseWriter,
            AllowCachingResponses = false,
        };

        options.ResultStatusCodes[HealthStatus.Healthy] = StatusCodes.Status200OK;
        options.ResultStatusCodes[HealthStatus.Degraded] = StatusCodes.Status200OK;
        options.ResultStatusCodes[HealthStatus.Unhealthy] = gating
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK;

        return options;
    }
}
