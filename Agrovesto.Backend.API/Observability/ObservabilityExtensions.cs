using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;

namespace Agrovesto.Backend.API.Observability;

/// <summary>
/// OpenTelemetry wiring for the NomaOS Onboarding API.
///
/// Everything is driven by the standard <c>OTEL_*</c> environment variables
/// (see <see cref="ObservabilityOptions"/>); there is no exporter URL anywhere
/// in appsettings. Signals leave the process as OTLP and land in Grafana Alloy,
/// which fans them out to Tempo (traces), Prometheus (metrics) and Loki (logs).
///
/// Two deliberate behaviours:
///  1. No OTLP endpoint configured => exporters are not registered at all. The
///     instrumentation still runs (cheap, and /status can report it), but the
///     SDK never tries to reach localhost:4317 and flood the logs.
///  2. Probe paths (/health*, /status, /hangfire) are filtered before span
///     creation — see <see cref="ObservabilityPaths"/>.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Registers tracing + metrics. Call before <c>builder.Build()</c>.
    /// </summary>
    public static IServiceCollection AddNomaosObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var options = ObservabilityOptions.FromConfiguration(
            configuration,
            environment.EnvironmentName,
            typeof(ObservabilityExtensions).Assembly);

        return services.AddNomaosObservability(options);
    }

    /// <summary>
    /// Overload used by the tests (and by anyone who wants to build the options
    /// themselves) so the wiring can be exercised without an <see cref="IHostEnvironment"/>.
    /// </summary>
    public static IServiceCollection AddNomaosObservability(
        this IServiceCollection services,
        ObservabilityOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<NomaosTelemetry>();

        if (!options.InstrumentationEnabled)
        {
            return services;
        }

        // W3C TraceContext + Baggage end to end. The .NET SDK does not read
        // OTEL_PROPAGATORS itself, so we honour it here.
        Sdk.SetDefaultTextMapPropagator(CreatePropagator(options));

        var redisRegistered = services.Any(static descriptor =>
            descriptor.ServiceType == typeof(IConnectionMultiplexer));

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => ConfigureResource(resource, options))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(NomaosTelemetry.ActivitySourceName)
                    // Probe spam stays out of Tempo — see ShouldTraceRequest.
                    .AddAspNetCoreInstrumentation(instrumentation =>
                        instrumentation.Filter = ShouldTraceRequest)
                    .AddHttpClientInstrumentation(instrumentation =>
                        instrumentation.FilterHttpRequestMessage = ShouldTraceOutboundRequest)
                    // SetDbStatementForText is left at its default (false):
                    // Microsoft.Data.SqlClient CommandText carries farmer PII
                    // and BVN/NIN values in this codebase, so only the
                    // sanitized attributes (db.system, db.name, server.address,
                    // and the stored-procedure/collection name) are exported.
                    .AddSqlClientInstrumentation();

                if (redisRegistered)
                {
                    // Only when the app actually registers a multiplexer —
                    // AddStackExchangeRedisCache alone does not, and the
                    // instrumentation would otherwise warn on every start.
                    tracing.AddRedisInstrumentation();
                }

                if (options.ExportersEnabled)
                {
                    // Parameterless: endpoint/protocol/headers/timeout all come
                    // from OTEL_EXPORTER_OTLP_* — no hardcoded collector URL.
                    tracing.AddOtlpExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(NomaosTelemetry.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation();

                if (options.ExportersEnabled)
                {
                    metrics.AddOtlpExporter();
                }
            });

        return services;
    }

    /// <summary>
    /// Adds the OTLP log exporter to the MEL provider chain. Serilog forwards
    /// to these providers (writeToProviders: true), so a single Serilog call
    /// produces both the JSON stdout line and the OTLP log record — with the
    /// same trace_id on both sides.
    /// </summary>
    public static ILoggingBuilder AddNomaosOpenTelemetryLogging(
        this ILoggingBuilder logging,
        ObservabilityOptions options)
    {
        ArgumentNullException.ThrowIfNull(logging);
        ArgumentNullException.ThrowIfNull(options);

        if (!options.ExportersEnabled)
        {
            return logging;
        }

        logging.AddOpenTelemetry(telemetry =>
        {
            telemetry.IncludeScopes = true;
            telemetry.IncludeFormattedMessage = true;
            telemetry.ParseStateValues = true;
            telemetry.SetResourceBuilder(BuildResourceBuilder(options));
            telemetry.AddOtlpExporter();
        });

        return logging;
    }

    /// <summary>
    /// Demo endpoint proving the manual-span + custom-metric path works
    /// end to end. Hit it after a deploy and you should see one
    /// <c>onboarding.demo</c> span in Tempo and the
    /// <c>nomaos_onboarding_steps_total</c> counter move in Prometheus.
    /// </summary>
    public static IEndpointRouteBuilder MapObservabilityDemoEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/internal/telemetry/demo")
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(pattern, HandleDemoAsync)
            .AllowAnonymous()
            .WithName("NomaosTelemetryDemo");

        return endpoints;
    }

    /// <summary>
    /// Inbound trace filter. A named method rather than a lambda so the rule is
    /// directly unit-testable — this predicate is the only thing standing
    /// between Tempo and several million probe spans a week.
    /// </summary>
    internal static bool ShouldTraceRequest(HttpContext context) =>
        !ObservabilityPaths.IsExcluded(context.Request.Path);

    /// <summary>Outbound (HttpClient) trace filter.</summary>
    internal static bool ShouldTraceOutboundRequest(HttpRequestMessage request) =>
        !ObservabilityPaths.IsExcluded(request.RequestUri);

    /// <summary>
    /// Handler behind the demo endpoint: one manual parent span, one child
    /// span, one log line carrying the trace id, and the custom
    /// counter/histogram pair recorded by <see cref="NomaosTelemetry.TrackAsync"/>.
    /// </summary>
    internal static async Task<IResult> HandleDemoAsync(
        NomaosTelemetry telemetry,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var logger = loggerFactory.CreateLogger("Nomaos.Telemetry.Demo");

        var payload = await telemetry.TrackAsync(
            "demo",
            async (activity, token) =>
            {
                activity?.SetTag("nomaos.demo", true);
                activity?.AddEvent(new ActivityEvent("demo.work.started"));

                // A tiny child span so the Tempo waterfall has depth.
                using (var child = telemetry.ActivitySource.StartActivity("onboarding.demo.child"))
                {
                    child?.SetTag("nomaos.child", true);
                    await Task.Delay(5, token).ConfigureAwait(false);
                }

                logger.LogInformation(
                    "Telemetry demo executed traceId={TraceId}",
                    Activity.Current?.TraceId.ToHexString() ?? "none");

                return new DemoResponse(
                    Activity.Current?.TraceId.ToHexString(),
                    Activity.Current?.SpanId.ToHexString(),
                    new[] { "nomaos.onboarding.steps", "nomaos.onboarding.step.duration" });
            },
            cancellationToken).ConfigureAwait(false);

        return Results.Json(payload);
    }

    internal static TextMapPropagator CreatePropagator(ObservabilityOptions options)
    {
        var propagators = new List<TextMapPropagator>();

        foreach (var name in options.Propagators)
        {
            switch (name)
            {
                case "tracecontext":
                    propagators.Add(new TraceContextPropagator());
                    break;
                case "baggage":
                    propagators.Add(new BaggagePropagator());
                    break;
                case "b3":
                case "b3multi":
                    // Not wired: Alloy/Tempo are W3C-native here. Listing the
                    // value is not an error, it is simply ignored.
                    break;
                default:
                    break;
            }
        }

        if (propagators.Count == 0)
        {
            propagators.Add(new TraceContextPropagator());
            propagators.Add(new BaggagePropagator());
        }

        return new CompositeTextMapPropagator(propagators);
    }

    internal static ResourceBuilder BuildResourceBuilder(ObservabilityOptions options)
    {
        var builder = ResourceBuilder.CreateDefault();
        ConfigureResource(builder, options);
        return builder;
    }

    internal static void ConfigureResource(ResourceBuilder resource, ObservabilityOptions options)
    {
        resource
            .AddService(
                serviceName: options.ServiceName,
                serviceNamespace: options.ServiceNamespace,
                serviceVersion: options.ServiceVersion,
                autoGenerateServiceInstanceId: false,
                serviceInstanceId: options.ServiceInstanceId)
            .AddAttributes(options.ResourceAttributes());
    }
}

/// <summary>Response body of the telemetry demo endpoint.</summary>
public sealed record DemoResponse(string? TraceId, string? SpanId, IReadOnlyList<string> Recorded);
