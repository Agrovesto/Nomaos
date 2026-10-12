using Agrovesto.Backend.API.Observability;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace NomaOS.Test.Observability;

public class ObservabilityOptionsTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void FromConfiguration_ReadsTheStandardOtelBlock()
    {
        var options = ObservabilityOptions.FromConfiguration(
            Config(
                ("OTEL_SERVICE_NAME", "nomaos-onboarding-api"),
                ("OTEL_SERVICE_NAMESPACE", "agrovesto"),
                ("OTEL_SERVICE_INSTANCE_ID", "i-0abc123"),
                ("OTEL_EXPORTER_OTLP_ENDPOINT", " http://alloy.monitoring.internal:4317 "),
                ("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc"),
                ("OTEL_PROPAGATORS", "tracecontext,baggage"),
                ("GIT_SHA", "abc1234"),
                ("DEPLOYMENT_ENVIRONMENT", "production")),
            "Production");

        options.ServiceName.Should().Be("nomaos-onboarding-api");
        options.ServiceNamespace.Should().Be("agrovesto");
        options.ServiceInstanceId.Should().Be("i-0abc123");
        options.OtlpEndpoint.Should().Be("http://alloy.monitoring.internal:4317");
        options.OtlpProtocol.Should().Be("grpc");
        options.GitSha.Should().Be("abc1234");
        options.DeploymentEnvironment.Should().Be("production");
        options.Propagators.Should().Equal("tracecontext", "baggage");
        options.InstrumentationEnabled.Should().BeTrue();
        options.ExportersEnabled.Should().BeTrue();
    }

    [Fact]
    public void FromConfiguration_FallsBackToDocumentedDefaults()
    {
        var options = ObservabilityOptions.FromConfiguration(Config(), "Staging");

        options.ServiceName.Should().Be(ObservabilityOptions.DefaultServiceName);
        options.ServiceNamespace.Should().Be(ObservabilityOptions.DefaultServiceNamespace);
        options.DeploymentEnvironment.Should().Be("Staging");
        options.GitSha.Should().Be("unknown");
        options.OtlpProtocol.Should().Be("grpc");
        options.ServiceInstanceId.Should().Be(Environment.MachineName);
        options.Propagators.Should().Equal("tracecontext", "baggage");
    }

    [Fact]
    public void FromConfiguration_WithoutEndpoint_DisablesExportersButKeepsInstrumentation()
    {
        var options = ObservabilityOptions.FromConfiguration(Config(("OTEL_EXPORTER_OTLP_ENDPOINT", "   ")), "Development");

        options.OtlpEndpoint.Should().BeNull();
        options.ExportersEnabled.Should().BeFalse();
        options.InstrumentationEnabled.Should().BeTrue();
    }

    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    [InlineData("1")]
    public void FromConfiguration_HonoursOtelSdkDisabled(string value)
    {
        var options = ObservabilityOptions.FromConfiguration(
            Config(("OTEL_SDK_DISABLED", value), ("OTEL_EXPORTER_OTLP_ENDPOINT", "http://alloy:4317")),
            "Production");

        options.SdkDisabled.Should().BeTrue();
        options.InstrumentationEnabled.Should().BeFalse();
        options.ExportersEnabled.Should().BeFalse();
    }

    [Theory]
    [InlineData("false")]
    [InlineData("0")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("yes")]
    public void IsTrue_OnlyAcceptsTrueAndOne(string? value)
    {
        ObservabilityOptions.IsTrue(value).Should().BeFalse();
    }

    [Fact]
    public void FromConfiguration_PrefersGitShaOverCommitSha()
    {
        ObservabilityOptions
            .FromConfiguration(Config(("COMMIT_SHA", "from-build-arg")), "Production")
            .GitSha.Should().Be("from-build-arg");

        ObservabilityOptions
            .FromConfiguration(Config(("GIT_SHA", "from-env"), ("COMMIT_SHA", "from-build-arg")), "Production")
            .GitSha.Should().Be("from-env");
    }

    [Fact]
    public void FromConfiguration_UsesHostnameForInstanceIdInsideContainers()
    {
        ObservabilityOptions
            .FromConfiguration(Config(("HOSTNAME", "7f3c9d1a2b4e")), "Production")
            .ServiceInstanceId.Should().Be("7f3c9d1a2b4e");
    }

    [Theory]
    [InlineData("tracecontext", new[] { "tracecontext" })]
    [InlineData("tracecontext, baggage", new[] { "tracecontext", "baggage" })]
    [InlineData("TraceContext,TRACECONTEXT", new[] { "tracecontext" })]
    [InlineData("b3multi,tracecontext", new[] { "b3multi", "tracecontext" })]
    public void ParsePropagators_NormalisesTheList(string raw, string[] expected)
    {
        ObservabilityOptions.ParsePropagators(raw).Should().Equal(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",,,")]
    public void ParsePropagators_FallsBackToW3C(string? raw)
    {
        ObservabilityOptions.ParsePropagators(raw).Should().Equal("tracecontext", "baggage");
    }

    [Fact]
    public void ResourceAttributes_CarryEnvironmentAndRevision()
    {
        var options = ObservabilityOptions.FromConfiguration(
            Config(("GIT_SHA", "deadbeef"), ("DEPLOYMENT_ENVIRONMENT", "production")),
            "Production");

        var attributes = options.ResourceAttributes().ToDictionary(pair => pair.Key, pair => pair.Value);

        attributes.Should().ContainKey("service.namespace").WhoseValue.Should().Be("agrovesto");
        attributes.Should().ContainKey("deployment.environment").WhoseValue.Should().Be("production");
        attributes.Should().ContainKey("vcs.repository.ref.revision").WhoseValue.Should().Be("deadbeef");
        attributes.Should().ContainKey("host.name");
        attributes.Should().ContainKey("service.version");
        attributes.Should().ContainKey("service.instance.id");
    }

    [Fact]
    public void ResolveVersion_StripsTheSourceLinkSuffix()
    {
        ObservabilityOptions.ResolveVersion(typeof(ObservabilityOptions).Assembly)
            .Should().NotBeNullOrWhiteSpace().And.NotContain("+");
    }

    [Fact]
    public void FromConfiguration_RejectsNullConfiguration()
    {
        var act = () => ObservabilityOptions.FromConfiguration(null!, "Production");
        act.Should().Throw<ArgumentNullException>();
    }
}
