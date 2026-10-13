using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Agrovesto.Backend.API.Health;

/// <summary>
/// Readiness probe for HashiCorp Vault.
///
/// Vault is a hard dependency of this service: with ASPNETCORE_ENVIRONMENT=
/// Production the app takes 100% of its configuration (connection strings, JWT
/// key, AWS keys, SendGrid, …) from <c>kv/nomaos/&lt;env&gt;/appsettings</c>. A
/// sealed Vault therefore means "do not send this instance traffic", which is
/// precisely a readiness condition — never a liveness one, because restarting
/// the container does not unseal anything.
///
/// Uses the unauthenticated <c>/v1/sys/health</c> endpoint, so the probe needs
/// no token and cannot itself expire. <c>standbyok=true</c> keeps HA standby
/// nodes (429) from being reported as failures.
/// </summary>
public sealed class VaultReadinessHealthCheck : IHealthCheck
{
    public const string Name = "vault";
    public const string HttpClientName = "health";

    private const int PerformanceStandby = 473;
    private const int DrSecondary = 472;
    private const int Uninitialized = 501;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public VaultReadinessHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var address = _configuration["VAULT_ADDR"] ?? _configuration["Vault:Address"];

        if (string.IsNullOrWhiteSpace(address))
        {
            // Development runs without Vault by design (Program.cs skips the
            // provider when IsDevelopment()), so this must not fail readiness.
            return HealthCheckResult.Degraded("VAULT_ADDR is not configured; Vault-backed configuration is disabled.");
        }

        if (!Uri.TryCreate(address.TrimEnd('/') + "/v1/sys/health?standbyok=true&perfstandbyok=true", UriKind.Absolute, out var probeUri))
        {
            return HealthCheckResult.Unhealthy($"VAULT_ADDR is not a valid absolute URI: '{address}'.");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, probeUri);
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            var data = Data(address, stopwatch, (int)response.StatusCode);

            return (int)response.StatusCode switch
            {
                (int)HttpStatusCode.OK => HealthCheckResult.Healthy("Vault is unsealed and active.", data),
                (int)HttpStatusCode.TooManyRequests => HealthCheckResult.Healthy("Vault is unsealed (standby).", data),
                DrSecondary => HealthCheckResult.Healthy("Vault DR secondary is active.", data),
                PerformanceStandby => HealthCheckResult.Healthy("Vault performance standby is active.", data),
                (int)HttpStatusCode.ServiceUnavailable => HealthCheckResult.Unhealthy("Vault is sealed.", data: data),
                Uninitialized => HealthCheckResult.Unhealthy("Vault is not initialized.", data: data),
                _ => HealthCheckResult.Degraded($"Unexpected Vault status code {(int)response.StatusCode}.", data: data),
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return HealthCheckResult.Degraded("Vault probe cancelled.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return HealthCheckResult.Unhealthy("Vault is not reachable.", ex, Data(address, stopwatch, 0));
        }
    }

    private static Dictionary<string, object> Data(string address, Stopwatch stopwatch, int statusCode) =>
        new(StringComparer.Ordinal)
        {
            ["address"] = address,
            ["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
            ["statusCode"] = statusCode,
        };
}
