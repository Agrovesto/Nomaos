using System.Net;
using System.Text.Json;
using Agrovesto.Backend.API.Health;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NomaOS.Test.TestSupport;
using StackExchange.Redis;

namespace NomaOS.Test.Health;

/// <summary>
/// The deployment gate, end to end.
///
/// deploy.sh polls /health/live and then /health/ready and rolls back when
/// either misbehaves, so the status-code matrix below is effectively part of
/// the deployment contract. It is exercised over a real HTTP pipeline via
/// TestServer — no sockets, no SQL, no Redis, no Vault.
/// </summary>
public class HealthEndpointsIntegrationTests
{
    private const string RedisConnection = "redis:6379";
    private const string VaultAddress = "https://vault.agrovesto.internal:8200";

    private static async Task<IHost> StartHostAsync(
        IDistributedCache? cache = null,
        HttpStatusCode vaultStatus = HttpStatusCode.OK,
        Exception? vaultFailure = null,
        bool configured = true)
    {
        var handler = vaultFailure is null
            ? new StubHttpMessageHandler(vaultStatus)
            : StubHttpMessageHandler.Throwing(vaultFailure);

        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Redis:ConnectionString"] = configured ? RedisConnection : null,
                            ["VAULT_ADDR"] = configured ? VaultAddress : null,
                            ["GIT_SHA"] = "abc1234",
                        }))
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddHealthChecks().AddNomaosProbeChecks();

                        // Registered last on purpose: AddNomaosHealthInfrastructure
                        // uses TryAdd/AddHttpClient, so these fakes win.
                        services.AddSingleton(cache ?? new StubDistributedCache());
                        services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(handler));
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapNomaosHealthEndpoints());
                    });
            })
            .StartAsync();

        return host;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> GetAsync(IHost host, string path)
    {
        using var response = await host.GetTestClient().GetAsync(path);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        response.Content.Headers.ContentType!.ToString().Should().Be("application/json; charset=utf-8");
        response.Headers.CacheControl!.NoStore.Should().BeTrue("a cached probe response can keep a dead instance in rotation");

        return (response.StatusCode, json);
    }

    [Fact]
    public async Task AllDependenciesHealthy_EveryEndpointReturns200()
    {
        using var host = await StartHostAsync();

        var (liveStatus, live) = await GetAsync(host, HealthEndpoints.Live);
        var (readyStatus, ready) = await GetAsync(host, HealthEndpoints.Ready);
        var (legacyStatus, legacy) = await GetAsync(host, HealthEndpoints.Legacy);
        var (statusStatus, status) = await GetAsync(host, HealthEndpoints.Status);

        liveStatus.Should().Be(HttpStatusCode.OK);
        live.GetProperty("status").GetString().Should().Be("Healthy");

        readyStatus.Should().Be(HttpStatusCode.OK);
        ready.GetProperty("ready").GetBoolean().Should().BeTrue();
        ready.GetProperty("checks").EnumerateArray().Select(check => check.GetProperty("name").GetString())
            .Should().BeEquivalentTo(new[] { "redis", "vault" });

        legacyStatus.Should().Be(HttpStatusCode.OK);
        legacy.GetProperty("checks").GetArrayLength().Should().Be(3);

        statusStatus.Should().Be(HttpStatusCode.OK);
        status.GetProperty("status").GetString().Should().Be("Healthy");
        status.GetProperty("build").GetProperty("gitSha").GetString().Should().Be("abc1234");

        await host.StopAsync();
    }

    [Fact]
    public async Task LivenessNeverTouchesADependency()
    {
        var cache = new StubDistributedCache { FailWith = new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down") };
        using var host = await StartHostAsync(cache, vaultFailure: new HttpRequestException("unreachable"));

        var (statusCode, body) = await GetAsync(host, HealthEndpoints.Live);

        statusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("status").GetString().Should().Be("Healthy");
        cache.Reads.Should().BeEmpty("liveness must not call Redis");

        await host.StopAsync();
    }

    [Fact]
    public async Task DependenciesDown_LiveStays200WhileReadyReturns503()
    {
        var cache = new StubDistributedCache { FailWith = new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down") };
        using var host = await StartHostAsync(cache, vaultFailure: new HttpRequestException("unreachable"));

        var (liveStatus, _) = await GetAsync(host, HealthEndpoints.Live);
        var (readyStatus, ready) = await GetAsync(host, HealthEndpoints.Ready);
        var (legacyStatus, _) = await GetAsync(host, HealthEndpoints.Legacy);
        var (statusStatus, status) = await GetAsync(host, HealthEndpoints.Status);

        liveStatus.Should().Be(HttpStatusCode.OK, "restarting the container cannot fix Redis");
        readyStatus.Should().Be(HttpStatusCode.ServiceUnavailable);
        ready.GetProperty("status").GetString().Should().Be("Unhealthy");
        ready.GetProperty("ready").GetBoolean().Should().BeFalse();

        legacyStatus.Should().Be(HttpStatusCode.ServiceUnavailable);

        // /status reports, it never gates — on-call must still get the body.
        statusStatus.Should().Be(HttpStatusCode.OK);
        status.GetProperty("status").GetString().Should().Be("Unhealthy");
        status.GetProperty("live").GetBoolean().Should().BeTrue();
        status.GetProperty("ready").GetBoolean().Should().BeFalse();
        status.GetProperty("summary").GetProperty("unhealthy").GetInt32().Should().Be(2);

        await host.StopAsync();
    }

    [Fact]
    public async Task SealedVault_MakesTheInstanceNotReady()
    {
        using var host = await StartHostAsync(vaultStatus: HttpStatusCode.ServiceUnavailable);

        var (readyStatus, ready) = await GetAsync(host, HealthEndpoints.Ready);

        readyStatus.Should().Be(HttpStatusCode.ServiceUnavailable);
        ready.GetProperty("checks").EnumerateArray()
            .Single(check => check.GetProperty("name").GetString() == "vault")
            .GetProperty("description").GetString().Should().Contain("sealed");

        await host.StopAsync();
    }

    [Fact]
    public async Task DegradedDependencies_StillServeTraffic()
    {
        // Nothing configured: the Development shape, where Vault and Redis are
        // simply absent. That is Degraded, and Degraded must stay in rotation.
        using var host = await StartHostAsync(configured: false);

        var (readyStatus, ready) = await GetAsync(host, HealthEndpoints.Ready);
        var (statusStatus, status) = await GetAsync(host, HealthEndpoints.Status);

        readyStatus.Should().Be(HttpStatusCode.OK);
        ready.GetProperty("status").GetString().Should().Be("Degraded");
        ready.GetProperty("ready").GetBoolean().Should().BeTrue();

        statusStatus.Should().Be(HttpStatusCode.OK);
        status.GetProperty("summary").GetProperty("degraded").GetInt32().Should().Be(2);
        status.GetProperty("ready").GetBoolean().Should().BeTrue();

        await host.StopAsync();
    }

    [Fact]
    public async Task StatusExposesUptimeMemoryAndVersion()
    {
        using var host = await StartHostAsync();

        var (_, status) = await GetAsync(host, HealthEndpoints.Status);

        var build = status.GetProperty("build");
        build.GetProperty("application").GetString().Should().Be(BuildInfo.ApplicationName);
        build.GetProperty("version").GetString().Should().NotBeNullOrWhiteSpace();
        build.GetProperty("environment").GetString().Should().NotBeNullOrWhiteSpace();
        build.GetProperty("uptimeSeconds").GetDouble().Should().BeGreaterThanOrEqualTo(0);
        build.GetProperty("uptime").GetString().Should().MatchRegex(@"^\d+d \d{2}h \d{2}m \d{2}s$");

        status.GetProperty("memory").GetProperty("workingSetMb").GetDouble().Should().BeGreaterThan(0);
        status.GetProperty("memory").GetProperty("threadCount").GetInt32().Should().BeGreaterThan(0);

        status.GetProperty("checks").EnumerateArray()
            .Select(check => check.GetProperty("durationMs").GetDouble())
            .Should().OnlyContain(duration => duration >= 0, "per-check latency is part of the on-call view");

        await host.StopAsync();
    }

    [Fact]
    public async Task ProbeEndpointsAreAnonymous()
    {
        using var host = await StartHostAsync();

        using var response = await host.GetTestClient().GetAsync(HealthEndpoints.Ready);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        await host.StopAsync();
    }
}
