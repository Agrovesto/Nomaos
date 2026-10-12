using System.Diagnostics;
using Agrovesto.Backend.API.Observability;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;

namespace NomaOS.Test.Observability;

public class ObservabilityExtensionsTests
{
    private static ObservabilityOptions Options(string? endpoint = null, bool disabled = false) =>
        new()
        {
            ServiceName = "nomaos-onboarding-api",
            ServiceNamespace = "agrovesto",
            ServiceVersion = "1.2.3",
            ServiceInstanceId = "i-0abc",
            DeploymentEnvironment = "Test",
            GitSha = "abc1234",
            OtlpEndpoint = endpoint,
            SdkDisabled = disabled,
        };

    [Fact]
    public void AddNomaosObservability_RegistersTelemetryAndOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddNomaosObservability(Options());

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ObservabilityOptions>().ServiceName.Should().Be("nomaos-onboarding-api");
        provider.GetRequiredService<NomaosTelemetry>().Should().NotBeNull();
    }

    [Fact]
    public void AddNomaosObservability_BuildsTracerAndMeterProvidersWithoutAnExporter()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddNomaosObservability(Options(endpoint: null));

        using var provider = services.BuildServiceProvider();

        // Resolving the providers forces the SDK to materialise every
        // instrumentation option (including the probe-path filters), which is
        // the part that would otherwise only ever run in production.
        provider.GetService<TracerProvider>().Should().NotBeNull();
        provider.GetService<MeterProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddNomaosObservability_WithEndpoint_RegistersOtlpExporters()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var act = () => services.AddNomaosObservability(Options(endpoint: "http://alloy.monitoring.internal:4317"));

        // Registration only — building the provider here would start an
        // exporter that tries to reach a collector that does not exist in CI.
        act.Should().NotThrow();
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(TracerProvider));
    }

    [Fact]
    public void AddNomaosObservability_WhenSdkDisabled_SkipsAllWiring()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddNomaosObservability(Options(endpoint: "http://alloy:4317", disabled: true));

        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(TracerProvider));

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<NomaosTelemetry>().Should().NotBeNull("the app still records spans locally");
    }

    [Fact]
    public void AddNomaosObservability_AddsRedisInstrumentationOnlyWhenAMultiplexerIsRegistered()
    {
        var withRedis = new ServiceCollection();
        withRedis.AddLogging();
        withRedis.AddSingleton(Mock.Of<IConnectionMultiplexer>());

        var act = () => withRedis.AddNomaosObservability(Options());

        act.Should().NotThrow();
    }

    [Fact]
    public void AddNomaosObservability_ConfigurationOverload_ReadsTheEnvironmentName()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OTEL_SERVICE_NAME"] = "from-config" })
            .Build();

        services.AddNomaosObservability(configuration, new StubHostEnvironment("Staging"));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<ObservabilityOptions>();

        options.ServiceName.Should().Be("from-config");
        options.DeploymentEnvironment.Should().Be("Staging");
    }

    [Fact]
    public void AddNomaosObservability_ValidatesItsArguments()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var nullServices = () => ((IServiceCollection)null!).AddNomaosObservability(Options());
        var nullOptions = () => services.AddNomaosObservability((ObservabilityOptions)null!);
        var nullConfiguration = () => services.AddNomaosObservability(null!, new StubHostEnvironment("Test"));
        var nullEnvironment = () => services.AddNomaosObservability(configuration, null!);

        nullServices.Should().Throw<ArgumentNullException>();
        nullOptions.Should().Throw<ArgumentNullException>();
        nullConfiguration.Should().Throw<ArgumentNullException>();
        nullEnvironment.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddNomaosOpenTelemetryLogging_IsANoOpWithoutACollector()
    {
        var services = new ServiceCollection();
        var before = services.Count;

        services.AddLogging(logging => logging.AddNomaosOpenTelemetryLogging(Options(endpoint: null)));

        services.Count.Should().BeGreaterThan(0);
        before.Should().Be(0);
    }

    [Fact]
    public void AddNomaosOpenTelemetryLogging_RegistersTheOtlpLogExporterWhenConfigured()
    {
        var services = new ServiceCollection();

        var act = () => services.AddLogging(logging =>
            logging.AddNomaosOpenTelemetryLogging(Options(endpoint: "http://alloy.monitoring.internal:4317")));

        act.Should().NotThrow();
    }

    [Fact]
    public void AddNomaosOpenTelemetryLogging_ValidatesItsArguments()
    {
        var services = new ServiceCollection();

        var nullBuilder = () => ((ILoggingBuilder)null!).AddNomaosOpenTelemetryLogging(Options());
        var nullOptions = () => services.AddLogging(logging => logging.AddNomaosOpenTelemetryLogging(null!));

        nullBuilder.Should().Throw<ArgumentNullException>();
        nullOptions.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("/health/live", false)]
    [InlineData("/status", false)]
    [InlineData("/hangfire/recurring", false)]
    [InlineData("/api/v2/user/ping", true)]
    public void ShouldTraceRequest_KeepsProbeTrafficOutOfTempo(string path, bool expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        ObservabilityExtensions.ShouldTraceRequest(context).Should().Be(expected);
    }

    [Theory]
    [InlineData("http://127.0.0.1:8080/health/status", false)]
    [InlineData("https://v3.api.termii.com/api/get-balance", true)]
    public void ShouldTraceOutboundRequest_FiltersSelfProbes(string uri, bool expected)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);

        ObservabilityExtensions.ShouldTraceOutboundRequest(request).Should().Be(expected);
    }

    [Fact]
    public void CreatePropagator_DefaultsToW3CTraceContextPlusBaggage()
    {
        var propagator = ObservabilityExtensions.CreatePropagator(Options());

        propagator.Should().BeOfType<CompositeTextMapPropagator>();
        propagator.Fields.Should().Contain("traceparent").And.Contain("baggage");
    }

    [Fact]
    public void CreatePropagator_HonoursAnExplicitPropagatorList()
    {
        var traceContextOnly = ObservabilityExtensions.CreatePropagator(new ObservabilityOptions
        {
            Propagators = new[] { "tracecontext" },
        });

        traceContextOnly.Fields.Should().Contain("traceparent");
        traceContextOnly.Fields.Should().NotContain("baggage");
    }

    [Fact]
    public void CreatePropagator_IgnoresUnsupportedNamesAndNeverReturnsAnEmptyPropagator()
    {
        var propagator = ObservabilityExtensions.CreatePropagator(new ObservabilityOptions
        {
            Propagators = new[] { "b3", "b3multi", "jaeger" },
        });

        propagator.Fields.Should().Contain("traceparent").And.Contain("baggage");
    }

    [Fact]
    public void ConfigureResource_CarriesServiceIdentityAndRevision()
    {
        var resource = ObservabilityExtensions.BuildResourceBuilder(Options()).Build();

        // GroupBy first: the SDK merges several resources (default + service +
        // ours) and the merge order, not this test, decides which duplicate wins.
        var attributes = resource.Attributes
            .GroupBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Value?.ToString(), StringComparer.Ordinal);

        attributes["service.name"].Should().Be("nomaos-onboarding-api");
        attributes["service.version"].Should().Be("1.2.3");
        attributes["service.instance.id"].Should().Be("i-0abc");
        attributes["deployment.environment"].Should().Be("Test");
        attributes["vcs.repository.ref.revision"].Should().Be("abc1234");
    }

    [Fact]
    public async Task HandleDemoAsync_ProducesASpanAndReturnsTheTraceIdentifiers()
    {
        using var telemetry = new NomaosTelemetry(
            NomaosTelemetry.ActivitySourceName,
            NomaosTelemetry.MeterName);

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == NomaosTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);

        var result = await ObservabilityExtensions.HandleDemoAsync(
            telemetry,
            NullLoggerFactory.Instance,
            CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleDemoAsync_ValidatesItsArguments()
    {
        using var telemetry = new NomaosTelemetry("demo." + Guid.NewGuid().ToString("N"), "demo");

        var nullTelemetry = async () => await ObservabilityExtensions.HandleDemoAsync(null!, NullLoggerFactory.Instance, default);
        var nullLoggerFactory = async () => await ObservabilityExtensions.HandleDemoAsync(telemetry, null!, default);

        await nullTelemetry.Should().ThrowAsync<ArgumentNullException>();
        await nullLoggerFactory.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void MapObservabilityDemoEndpoint_ValidatesItsArguments()
    {
        var act = () => ((Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)null!).MapObservabilityDemoEndpoint();

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public StubHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "Agrovesto.Backend.API";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
