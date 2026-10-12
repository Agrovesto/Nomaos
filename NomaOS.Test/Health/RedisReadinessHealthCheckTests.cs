using Agrovesto.Backend.API.Health;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NomaOS.Test.TestSupport;
using StackExchange.Redis;

namespace NomaOS.Test.Health;

public class RedisReadinessHealthCheckTests
{
    private const string ConnectionString = "nomaos-cache.abc123.ng.0001.euw1.cache.amazonaws.com:6379,password=s3cr3t,ssl=True";

    private static IConfiguration Config(string? connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Redis:ConnectionString"] = connectionString })
            .Build();

    private static IServiceProvider Provider(IDistributedCache? cache)
    {
        var services = new ServiceCollection();
        if (cache is not null)
        {
            services.AddSingleton(cache);
        }

        return services.BuildServiceProvider();
    }

    private static HealthCheckContext Context() =>
        new()
        {
            Registration = new HealthCheckRegistration(
                RedisReadinessHealthCheck.Name,
                _ => new RedisReadinessHealthCheck(Provider(null), Config(null)),
                HealthStatus.Unhealthy,
                new[] { HealthTags.Ready }),
        };

    [Fact]
    public async Task CheckHealthAsync_IsHealthyWhenTheCacheAnswers()
    {
        var cache = new StubDistributedCache();
        var check = new RedisReadinessHealthCheck(Provider(cache), Config(ConnectionString));

        var result = await check.CheckHealthAsync(Context(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Redis responded.");
        cache.Reads.Should().Equal(RedisReadinessHealthCheck.ProbeKey);
        result.Data.Should().ContainKey("latencyMs");
    }

    [Fact]
    public async Task CheckHealthAsync_NeverLeaksTheRedisPassword()
    {
        var check = new RedisReadinessHealthCheck(Provider(new StubDistributedCache()), Config(ConnectionString));

        var result = await check.CheckHealthAsync(Context(), CancellationToken.None);

        result.Data["endpoint"].Should().Be("nomaos-cache.abc123.ng.0001.euw1.cache.amazonaws.com:6379");
        result.Data["endpoint"].ToString().Should().NotContain("s3cr3t");
    }

    [Fact]
    public async Task CheckHealthAsync_IsUnhealthyWhenRedisIsDown()
    {
        var cache = new StubDistributedCache
        {
            FailWith = new RedisConnectionException(ConnectionFailureType.UnableToConnect, "It was not possible to connect"),
        };

        var result = await new RedisReadinessHealthCheck(Provider(cache), Config(ConnectionString))
            .CheckHealthAsync(Context(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Redis is not reachable.");
        result.Exception.Should().BeOfType<RedisConnectionException>();
        result.Data["endpoint"].ToString().Should().NotContain("s3cr3t");
    }

    [Fact]
    public async Task CheckHealthAsync_IsDegradedWhenRedisIsNotConfigured()
    {
        var result = await new RedisReadinessHealthCheck(Provider(new StubDistributedCache()), Config(null))
            .CheckHealthAsync(Context(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("not configured");
    }

    [Fact]
    public async Task CheckHealthAsync_IsDegradedWhenNoCacheIsRegistered()
    {
        var result = await new RedisReadinessHealthCheck(Provider(null), Config(ConnectionString))
            .CheckHealthAsync(Context(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("No IDistributedCache");
    }

    [Fact]
    public async Task CheckHealthAsync_IsDegradedWhenTheProbeItselfIsCancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await new RedisReadinessHealthCheck(Provider(new StubDistributedCache()), Config(ConnectionString))
            .CheckHealthAsync(Context(), cts.Token);

        // A drain must not be reported as a dead dependency.
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("Redis probe cancelled.");
    }

    [Theory]
    [InlineData("localhost:6379,password=hunter2", "localhost:6379")]
    [InlineData("  redis:6379  ", "redis:6379")]
    [InlineData(",password=hunter2", "unset")]
    [InlineData("", "unset")]
    [InlineData("   ", "unset")]
    [InlineData(null, "unset")]
    public void Redact_KeepsOnlyTheEndpoint(string? connectionString, string expected)
    {
        RedisReadinessHealthCheck.Redact(connectionString).Should().Be(expected);
    }

    [Fact]
    public void Constructor_RejectsNullDependencies()
    {
        var nullProvider = () => new RedisReadinessHealthCheck(null!, Config(null));
        var nullConfiguration = () => new RedisReadinessHealthCheck(Provider(null), null!);

        nullProvider.Should().Throw<ArgumentNullException>();
        nullConfiguration.Should().Throw<ArgumentNullException>();
    }
}
