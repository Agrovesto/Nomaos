using System.Reflection;

namespace Agrovesto.Backend.API.Observability;

/// <summary>
/// Strongly typed view over the standard <c>OTEL_*</c> environment variables.
///
/// Deliberately the *only* place that reads telemetry configuration: no
/// exporter URL, service name or environment name is ever hardcoded in
/// appsettings.json. Compose passes the OTEL_* block (see
/// deploy/ec2/env.otel.example) and this class turns it into something the
/// SDK wiring — and the unit tests — can reason about.
///
/// Values are read through <see cref="IConfiguration"/> rather than
/// <c>Environment.GetEnvironmentVariable</c> so they can be supplied in tests
/// from an in-memory collection, and so a future config source (Vault) can
/// override them without touching this type.
/// </summary>
public sealed class ObservabilityOptions
{
    public const string DefaultServiceName = "nomaos-onboarding-api";
    public const string DefaultServiceNamespace = "agrovesto";

    /// <summary>OTEL_SERVICE_NAME. Falls back to the documented default.</summary>
    public string ServiceName { get; init; } = DefaultServiceName;

    /// <summary>service.namespace resource attribute.</summary>
    public string ServiceNamespace { get; init; } = DefaultServiceNamespace;

    /// <summary>Informational assembly version, surfaced as service.version.</summary>
    public string ServiceVersion { get; init; } = "0.0.0";

    /// <summary>service.instance.id — the container/host identity.</summary>
    public string ServiceInstanceId { get; init; } = "unknown";

    /// <summary>deployment.environment (Development / Staging / Production).</summary>
    public string DeploymentEnvironment { get; init; } = "Production";

    /// <summary>Full commit SHA of the running build (GIT_SHA / COMMIT_SHA).</summary>
    public string GitSha { get; init; } = "unknown";

    /// <summary>
    /// OTEL_EXPORTER_OTLP_ENDPOINT. Null/empty means "no collector reachable",
    /// which switches the exporters off entirely rather than letting the SDK
    /// default to http://localhost:4317 and log a connection error every 30s.
    /// </summary>
    public string? OtlpEndpoint { get; init; }

    /// <summary>OTEL_EXPORTER_OTLP_PROTOCOL — informational; the exporter reads it itself.</summary>
    public string OtlpProtocol { get; init; } = "grpc";

    /// <summary>OTEL_SDK_DISABLED=true kills all instrumentation (escape hatch).</summary>
    public bool SdkDisabled { get; init; }

    /// <summary>OTEL_PROPAGATORS, already split and normalised.</summary>
    public IReadOnlyList<string> Propagators { get; init; } = new[] { "tracecontext", "baggage" };

    /// <summary>Instrumentation is created (spans/metrics exist in-process).</summary>
    public bool InstrumentationEnabled => !SdkDisabled;

    /// <summary>
    /// OTLP export is wired. Requires both an enabled SDK and a configured
    /// endpoint — see <see cref="OtlpEndpoint"/>.
    /// </summary>
    public bool ExportersEnabled => !SdkDisabled && !string.IsNullOrWhiteSpace(OtlpEndpoint);

    public static ObservabilityOptions FromConfiguration(
        IConfiguration configuration,
        string environmentName,
        Assembly? entryAssembly = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var assembly = entryAssembly ?? Assembly.GetExecutingAssembly();

        return new ObservabilityOptions
        {
            ServiceName = FirstNonEmpty(configuration["OTEL_SERVICE_NAME"], DefaultServiceName),
            ServiceNamespace = FirstNonEmpty(configuration["OTEL_SERVICE_NAMESPACE"], DefaultServiceNamespace),
            ServiceVersion = ResolveVersion(assembly),
            ServiceInstanceId = FirstNonEmpty(
                configuration["OTEL_SERVICE_INSTANCE_ID"],
                configuration["HOSTNAME"],
                Environment.MachineName),
            DeploymentEnvironment = FirstNonEmpty(
                configuration["DEPLOYMENT_ENVIRONMENT"],
                environmentName,
                "Production"),
            GitSha = FirstNonEmpty(configuration["GIT_SHA"], configuration["COMMIT_SHA"], "unknown"),
            OtlpEndpoint = NullIfEmpty(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]),
            OtlpProtocol = FirstNonEmpty(configuration["OTEL_EXPORTER_OTLP_PROTOCOL"], "grpc"),
            SdkDisabled = IsTrue(configuration["OTEL_SDK_DISABLED"]),
            Propagators = ParsePropagators(configuration["OTEL_PROPAGATORS"]),
        };
    }

    /// <summary>
    /// Resource attributes this app always publishes. OTEL_RESOURCE_ATTRIBUTES
    /// is handled by the SDK's own environment detector and is merged on top of
    /// these, so an operator can always add or override attributes from Compose.
    /// </summary>
    public IEnumerable<KeyValuePair<string, object>> ResourceAttributes()
    {
        yield return new KeyValuePair<string, object>("service.namespace", ServiceNamespace);
        yield return new KeyValuePair<string, object>("deployment.environment", DeploymentEnvironment);
        yield return new KeyValuePair<string, object>("deployment.environment.name", DeploymentEnvironment);
        yield return new KeyValuePair<string, object>("service.version", ServiceVersion);
        yield return new KeyValuePair<string, object>("service.instance.id", ServiceInstanceId);
        yield return new KeyValuePair<string, object>("vcs.repository.ref.revision", GitSha);
        yield return new KeyValuePair<string, object>("host.name", Environment.MachineName);
    }

    internal static IReadOnlyList<string> ParsePropagators(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new[] { "tracecontext", "baggage" };
        }

        var parsed = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static value => value.ToLowerInvariant())
            .Distinct()
            .ToArray();

        return parsed.Length == 0
            ? new[] { "tracecontext", "baggage" }
            : parsed;
    }

    internal static bool IsTrue(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("1", StringComparison.Ordinal));

    internal static string ResolveVersion(Assembly assembly)
    {
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            // Strip the "+<sha>" source-link suffix the SDK appends.
            var plus = informational.IndexOf('+');
            return plus > 0 ? informational[..plus] : informational;
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }

    private static string FirstNonEmpty(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate.Trim();
            }
        }

        return string.Empty;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
