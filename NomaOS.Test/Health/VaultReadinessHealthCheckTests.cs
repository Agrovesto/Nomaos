using System.Net;
using Agrovesto.Backend.API.Health;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NomaOS.Test.TestSupport;

namespace NomaOS.Test.Health;

public class VaultReadinessHealthCheckTests
{
    private const string Address = "https://vault.agrovesto.internal:8200";

    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();

    private static HealthCheckContext Context() =>
        new()
        {
            Registration = new HealthCheckRegistration(
                VaultReadinessHealthCheck.Name,
                new VaultReadinessHealthCheck(
                    new StubHttpClientFactory(new StubHttpMessageHandler(HttpStatusCode.OK)),
                    Config()),
                HealthStatus.Unhealthy,
                new[] { HealthTags.Ready }),
        };

    private static async Task<(HealthCheckResult Result, StubHttpMessageHandler Handler, StubHttpClientFactory Factory)> RunAsync(
        HttpStatusCode statusCode,
        string address = Address)
    {
        var handler = new StubHttpMessageHandler(statusCode);
        var factory = new StubHttpClientFactory(handler);

        var result = await new VaultReadinessHealthCheck(factory, Config(("VAULT_ADDR", address)))
            .CheckHealthAsync(Context(), CancellationToken.None);

        return (result, handler, factory);
    }

    [Fact]
    public async Task CheckHealthAsync_HitsTheUnauthenticatedSysHealthEndpointOnTheNamedClient()
    {
        var (result, handler, factory) = await RunAsync(HttpStatusCode.OK, "https://vault.agrovesto.internal:8200/");

        result.Status.Should().Be(HealthStatus.Healthy);

        var request = handler.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri!.AbsolutePath.Should().Be("/v1/sys/health");
        request.RequestUri.Query.Should().Contain("standbyok=true").And.Contain("perfstandbyok=true");
        request.Headers.Authorization.Should().BeNull("the sys/health endpoint must not need a token");

        // Reusing the pooled "health" client keeps the probe off a fresh socket
        // every 10 seconds.
        factory.RequestedNames.Should().Equal(VaultReadinessHealthCheck.HttpClientName);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "unsealed and active")]
    [InlineData(HttpStatusCode.TooManyRequests, "standby")]
    [InlineData((HttpStatusCode)472, "DR secondary")]
    [InlineData((HttpStatusCode)473, "performance standby")]
    public async Task CheckHealthAsync_TreatsEveryServingVaultRoleAsHealthy(HttpStatusCode statusCode, string description)
    {
        var (result, _, _) = await RunAsync(statusCode);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain(description);
        result.Data["statusCode"].Should().Be((int)statusCode);
        result.Data["address"].Should().Be(Address);
        result.Data.Should().ContainKey("latencyMs");
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, "sealed")]
    [InlineData((HttpStatusCode)501, "not initialized")]
    public async Task CheckHealthAsync_IsUnhealthyWhenVaultCannotServeSecrets(HttpStatusCode statusCode, string description)
    {
        var (result, _, _) = await RunAsync(statusCode);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain(description);
    }

    [Fact]
    public async Task CheckHealthAsync_IsDegradedOnAnUnexpectedStatusCode()
    {
        var (result, _, _) = await RunAsync(HttpStatusCode.Forbidden);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("403");
    }

    [Fact]
    public async Task CheckHealthAsync_IsUnhealthyWhenVaultIsUnreachable()
    {
        var handler = StubHttpMessageHandler.Throwing(new HttpRequestException("No such host is known"));

        var result = await new VaultReadinessHealthCheck(new StubHttpClientFactory(handler), Config(("VAULT_ADDR", Address)))
            .CheckHealthAsync(Context(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Vault is not reachable.");
        result.Exception.Should().BeOfType<HttpRequestException>();
        result.Data["statusCode"].Should().Be(0);
    }

    [Fact]
    public async Task CheckHealthAsync_IsDegradedWhenTheProbeItselfIsCancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await new VaultReadinessHealthCheck(
                new StubHttpClientFactory(new StubHttpMessageHandler(HttpStatusCode.OK)),
                Config(("VAULT_ADDR", Address)))
            .CheckHealthAsync(Context(), cts.Token);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("Vault probe cancelled.");
    }

    [Fact]
    public async Task CheckHealthAsync_IsDegradedWhenVaultIsNotConfigured()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK);

        var result = await new VaultReadinessHealthCheck(new StubHttpClientFactory(handler), Config())
            .CheckHealthAsync(Context(), CancellationToken.None);

        // Development runs without Vault; readiness must not fail for that.
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("VAULT_ADDR is not configured");
        handler.Requests.Should().BeEmpty("no HTTP call may be made when Vault is disabled");
    }

    [Fact]
    public async Task CheckHealthAsync_FallsBackToTheVaultAddressConfigurationKey()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK);

        var result = await new VaultReadinessHealthCheck(
                new StubHttpClientFactory(handler),
                Config(("Vault:Address", Address)))
            .CheckHealthAsync(Context(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task CheckHealthAsync_IsUnhealthyWhenVaultAddrIsGarbage()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK);

        var result = await new VaultReadinessHealthCheck(
                new StubHttpClientFactory(handler),
                Config(("VAULT_ADDR", "not a uri")))
            .CheckHealthAsync(Context(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("not a valid absolute URI");
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_RejectsNullDependencies()
    {
        var nullFactory = () => new VaultReadinessHealthCheck(null!, Config());
        var nullConfiguration = () => new VaultReadinessHealthCheck(
            new StubHttpClientFactory(new StubHttpMessageHandler(HttpStatusCode.OK)),
            null!);

        nullFactory.Should().Throw<ArgumentNullException>();
        nullConfiguration.Should().Throw<ArgumentNullException>();
    }
}
