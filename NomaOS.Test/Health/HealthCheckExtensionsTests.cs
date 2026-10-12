using Agrovesto.Backend.API.Health;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace NomaOS.Test.Health;

public class HealthCheckExtensionsTests
{
    private static IServiceCollection Services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        return services;
    }

    [Fact]
    public void AddNomaosHealthInfrastructure_RegistersBuildInfoAndTheBoundedProbeClient()
    {
        var services = Services();

        services.AddNomaosHealthInfrastructure();

        using var provider = services.BuildServiceProvider();

        var buildInfo = provider.GetRequiredService<BuildInfo>();
        buildInfo.Application.Should().Be(BuildInfo.ApplicationName);
        // No IHostEnvironment registered -> the safe default, never "Development".
        buildInfo.Environment.Should().Be("Production");

        var client = provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient(VaultReadinessHealthCheck.HttpClientName);

        // A hung Vault must not outlive the load balancer's patience; the
        // default HttpClient timeout of 100s would do exactly that.
        client.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        client.DefaultRequestHeaders.UserAgent.ToString().Should().Contain("Agrovesto-HealthCheck");
    }

    [Fact]
    public void AddNomaosHealthInfrastructure_IsIdempotentAndRespectsAPreRegisteredBuildInfo()
    {
        var mine = new BuildInfo("mine", "1.0.0", "sha", "Staging", "host", DateTimeOffset.UtcNow);

        var services = Services();
        services.AddSingleton(mine);
        services.AddNomaosHealthInfrastructure();
        services.AddNomaosHealthInfrastructure();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<BuildInfo>().Should().BeSameAs(mine);
    }

    [Fact]
    public void AddNomaosHealthInfrastructure_UsesTheHostEnvironmentNameWhenAvailable()
    {
        var services = Services();
        services.AddSingleton<IHostEnvironment>(new StubEnvironment("Staging"));

        services.AddNomaosHealthInfrastructure();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<BuildInfo>().Environment.Should().Be("Staging");
    }

    [Fact]
    public void AddNomaosProbeChecks_RegistersTheThreeProbesWithTheRightTags()
    {
        var services = Services();

        services.AddHealthChecks().AddNomaosProbeChecks();

        using var provider = services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        var self = registrations.Single(r => r.Name == SelfLivenessHealthCheck.Name);
        var redis = registrations.Single(r => r.Name == RedisReadinessHealthCheck.Name);
        var vault = registrations.Single(r => r.Name == VaultReadinessHealthCheck.Name);

        // The single most important assertion in the health suite: nothing but
        // the self-check may be tagged "live", or a dependency outage would
        // restart every container in the fleet.
        self.Tags.Should().Equal(HealthTags.Live);
        redis.Tags.Should().Equal(HealthTags.Ready);
        vault.Tags.Should().Equal(HealthTags.Ready);

        redis.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        vault.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        self.FailureStatus.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public void CreateOptions_GatingEndpointsReturn503OnlyForUnhealthy()
    {
        var options = HealthCheckExtensions.CreateOptions(
            _ => true,
            HealthResponseWriters.WriteReadyAsync,
            gating: true);

        options.ResultStatusCodes[HealthStatus.Healthy].Should().Be(StatusCodes.Status200OK);
        options.ResultStatusCodes[HealthStatus.Degraded].Should().Be(StatusCodes.Status200OK);
        options.ResultStatusCodes[HealthStatus.Unhealthy].Should().Be(StatusCodes.Status503ServiceUnavailable);
        options.AllowCachingResponses.Should().BeFalse();
        options.ResponseWriter.Should().NotBeNull();
    }

    [Fact]
    public void CreateOptions_ReportingEndpointsAlwaysReturn200()
    {
        var options = HealthCheckExtensions.CreateOptions(
            _ => true,
            HealthResponseWriters.WriteStatusAsync,
            gating: false);

        // /status reports rather than gates: a dashboard that cannot read the
        // body during an incident is worthless.
        options.ResultStatusCodes[HealthStatus.Unhealthy].Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public void CreateOptions_KeepsThePredicate()
    {
        var options = HealthCheckExtensions.CreateOptions(
            registration => registration.Tags.Contains(HealthTags.Ready),
            HealthResponseWriters.WriteReadyAsync,
            gating: true);

        var ready = new HealthCheckRegistration("redis", new AlwaysHealthy(), null, new[] { HealthTags.Ready });
        var live = new HealthCheckRegistration("self", new AlwaysHealthy(), null, new[] { HealthTags.Live });

        options.Predicate!(ready).Should().BeTrue();
        options.Predicate!(live).Should().BeFalse();
    }

    [Fact]
    public void Extensions_RejectNullArguments()
    {
        var nullInfrastructure = () => ((IServiceCollection)null!).AddNomaosHealthInfrastructure();
        var nullBuilder = () => ((IHealthChecksBuilder)null!).AddNomaosProbeChecks();
        var nullEndpoints = () => ((Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)null!).MapNomaosHealthEndpoints();

        nullInfrastructure.Should().Throw<ArgumentNullException>();
        nullBuilder.Should().Throw<ArgumentNullException>();
        nullEndpoints.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task MapNomaosHealthEndpoints_MapsAllFourPaths()
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddHealthChecks().AddNomaosProbeChecks();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapNomaosHealthEndpoints());
                }))
            .StartAsync();

        var dataSource = host.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
        var patterns = dataSource.Endpoints
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .Select(endpoint => "/" + endpoint.RoutePattern.RawText!.TrimStart('/'))
            .ToArray();

        patterns.Should().Contain(new[]
        {
            HealthEndpoints.Live,
            HealthEndpoints.Ready,
            HealthEndpoints.Legacy,
            HealthEndpoints.Status,
        });

        await host.StopAsync();
    }

    private sealed class AlwaysHealthy : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(HealthCheckResult.Healthy());
    }

    private sealed class StubEnvironment : IHostEnvironment
    {
        public StubEnvironment(string environmentName) => EnvironmentName = environmentName;

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "Agrovesto.Backend.API";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
