using System.Diagnostics;
using System.Text.Json;
using Agrovesto.Backend.API.Observability;
using FluentAssertions;
using Serilog;
using Serilog.Events;

namespace NomaOS.Test.Observability;

/// <summary>
/// Proves the container actually emits one JSON object per line on stdout with
/// the fields Alloy/Loki are configured to parse. Anything less and the log
/// pipeline silently degrades to unparsed text.
/// </summary>
public class NomaosSerilogTests
{
    private static Func<string, string?> Env(Dictionary<string, string?> values) =>
        key => values.TryGetValue(key, out var value) ? value : null;

    private static Func<string, string?> NoCloudWatch(params (string Key, string? Value)[] values) =>
        Env(values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
    private static string CaptureConsole(Action action)
    {
        var original = Console.Out;
        var writer = new StringWriter();
        try
        {
            Console.SetOut(writer);
            action();
        }
        finally
        {
            Console.SetOut(original);
        }

        return writer.ToString();
    }

    /// <summary>
    /// Picks the emitted line that contains <paramref name="marker"/> instead of
    /// blindly taking the last one: stdout is process-global, so another test
    /// (or the SDK itself) writing a line must not be able to break this
    /// assertion.
    /// </summary>
    private static JsonElement ParseLineContaining(string output, string marker)
    {
        var line = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault(candidate => candidate.Contains(marker, StringComparison.Ordinal) && candidate.TrimStart().StartsWith("{", StringComparison.Ordinal));

        line.Should().NotBeNull("the JSON console sink must emit a line containing '{0}'", marker);

        return JsonDocument.Parse(line!).RootElement;
    }

    [Fact]
    public void Configure_WritesOneJsonObjectPerLineToStdout()
    {
        var output = CaptureConsole(() =>
        {
            var logger = NomaosSerilog
                .Configure(new LoggerConfiguration(), NoCloudWatch(("OTEL_SERVICE_NAME", "nomaos-onboarding-api")), "Production")
                .CreateLogger();

            logger.Information("farmer {FarmerId} onboarded", 4711);
            logger.Dispose();
        });

        var json = ParseLineContaining(output, "4711");

        json.GetProperty("Level").GetString().Should().Be("Information");
        json.GetProperty("MessageTemplate").GetString().Should().Contain("{FarmerId}");
        json.GetProperty("RenderedMessage").GetString().Should().Contain("4711");
        json.GetProperty("Properties").GetProperty("service.name").GetString().Should().Be("nomaos-onboarding-api");
    }

    [Fact]
    public void Configure_StampsEnvironmentAndGitShaOnEveryLine()
    {
        var output = CaptureConsole(() =>
        {
            var logger = NomaosSerilog
                .Configure(
                    new LoggerConfiguration(),
                    NoCloudWatch(("GIT_SHA", "abc1234"), ("DEPLOYMENT_ENVIRONMENT", "production")),
                    "Production")
                .CreateLogger();

            logger.Warning("disk almost full");
            logger.Dispose();
        });

        var properties = ParseLineContaining(output, "disk almost full").GetProperty("Properties");

        properties.GetProperty("git.sha").GetString().Should().Be("abc1234");
        properties.GetProperty("deployment.environment").GetString().Should().Be("production");
    }

    [Fact]
    public void Configure_CorrelatesLogsWithTheActiveTrace()
    {
        using var activity = new Activity("request");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var output = CaptureConsole(() =>
        {
            var logger = NomaosSerilog.Configure(new LoggerConfiguration(), NoCloudWatch(), "Production").CreateLogger();
            logger.Information("inside a span");
            logger.Dispose();
        });

        var properties = ParseLineContaining(output, "inside a span").GetProperty("Properties");

        properties.GetProperty("trace_id").GetString().Should().Be(activity.TraceId.ToHexString());
        properties.GetProperty("span_id").GetString().Should().Be(activity.SpanId.ToHexString());
    }

    [Fact]
    public void Configure_HonoursLogLevel()
    {
        var output = CaptureConsole(() =>
        {
            var logger = NomaosSerilog
                .Configure(new LoggerConfiguration(), NoCloudWatch(("LOG_LEVEL", "Warning")), "Production")
                .CreateLogger();

            logger.Information("should be filtered out");
            logger.Error("should survive");
            logger.Dispose();
        });

        output.Should().NotContain("should be filtered out");
        output.Should().Contain("should survive");
    }

    [Fact]
    public void Configure_WritesTheOptionalRollingFileWhenLogFilePathIsSet()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nomaos-log-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "nomaos-.log");

        try
        {
            CaptureConsole(() =>
            {
                var logger = NomaosSerilog
                    .Configure(new LoggerConfiguration(), NoCloudWatch(("LOG_FILE_PATH", path)), "Production")
                    .CreateLogger();

                logger.Information("written to the bind mount");
                logger.Dispose();
            });

            var files = Directory.GetFiles(directory);
            files.Should().NotBeEmpty();
            File.ReadAllText(files[0]).Should().Contain("written to the bind mount");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void CreateBootstrapLogger_EmitsJsonWithoutTouchingAws()
    {
        var output = CaptureConsole(() =>
        {
            var logger = NomaosSerilog.CreateBootstrapLogger();
            logger.Information("starting up");
            (logger as IDisposable)?.Dispose();
        });

        ParseLineContaining(output, "starting up")
            .GetProperty("RenderedMessage").GetString().Should().Contain("starting up");
    }

}
