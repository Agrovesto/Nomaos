using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Agrovesto.Backend.API.Observability;

/// <summary>
/// Builds the Serilog pipeline for the container: <b>JSON lines on stdout</b>.
///
/// Why JSON on stdout rather than a file:
/// Docker's json-file driver (and journald) captures stdout, and Grafana Alloy
/// scrapes that. One JSON object per line means Loki/Alloy can parse the fields
/// without a regex stage, and <see cref="ActivityTraceEnricher"/> puts
/// <c>trace_id</c> in every line so Loki can link straight into Tempo.
///
/// The file sink is retained but opt-in (LOG_FILE_PATH) because the systemd
/// deployment relied on it; a container that writes to a bind mount is fine,
/// a container that writes logs nobody rotates is not.
///
/// The AWS CloudWatch sink is likewise opt-in now (AWS_LOGS_ENABLED) instead of
/// unconditional: shipping the same line twice (CloudWatch + Loki) doubles the
/// bill for no benefit, and a missing IAM role used to make startup noisy.
/// It defaults to ON in Production only, preserving today's behaviour.
/// </summary>
public static class NomaosSerilog
{

    /// <summary>
    /// Console-only logger used between process start and host build, so the
    /// "starting up" / "Vault unreachable" lines are still JSON on stdout..
    /// </summary>
    public static Serilog.ILogger CreateBootstrapLogger() =>
        new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(new JsonFormatter(renderMessage: true))
            .CreateLogger();

    /// <summary>
    /// Configures <paramref name="configuration"/> from environment variables.
    /// </summary>
    /// <param name="configuration">The Serilog configuration to mutate.</param>
    /// <param name="getEnv">Environment accessor — injected so tests never touch the real environment.</param>
    /// <param name="environmentName">ASPNETCORE_ENVIRONMENT, used only to pick the CloudWatch default.</param>
    public static LoggerConfiguration Configure(
        LoggerConfiguration configuration,
        Func<string, string?> getEnv,
        string environmentName = "Production")
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(getEnv);

        var minimumLevel = ParseLevel(getEnv("LOG_LEVEL"), LogEventLevel.Information);

        configuration
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.With(new ActivityTraceEnricher())
            .Enrich.WithProperty("service.name", Coalesce(getEnv("OTEL_SERVICE_NAME"), ObservabilityOptions.DefaultServiceName))
            .Enrich.WithProperty("deployment.environment", Coalesce(getEnv("DEPLOYMENT_ENVIRONMENT"), environmentName))
            .Enrich.WithProperty("git.sha", Coalesce(getEnv("GIT_SHA"), getEnv("COMMIT_SHA"), "unknown"))
            // One JSON object per line. renderMessage:true keeps the rendered
            // text alongside the template so a human tailing `docker logs` can
            // still read it without a jq incantation.
            .WriteTo.Console(new JsonFormatter(renderMessage: true));

        var logFilePath = getEnv("LOG_FILE_PATH");
        if (!string.IsNullOrWhiteSpace(logFilePath))
        {
            configuration.WriteTo.File(
                formatter: new JsonFormatter(renderMessage: true),
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true);
        }

        return configuration;
    }

    internal static LogEventLevel ParseLevel(string? raw, LogEventLevel fallback) =>
        Enum.TryParse<LogEventLevel>(raw, ignoreCase: true, out var parsed) ? parsed : fallback;

    private static string Coalesce(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }
        return string.Empty;
    }
}
