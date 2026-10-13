using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Agrovesto.Backend.API.Observability;

/// <summary>
/// The application's own telemetry surface: one <see cref="ActivitySource"/>
/// for manual spans and one <see cref="Meter"/> for custom instruments.
///
/// Registered as a singleton so the instrument objects are created exactly once
/// (creating a Counter per request is a documented OTel anti-pattern — the SDK
/// keeps per-instrument state keyed by object identity).
///
/// Names are constants because the SDK only exports an ActivitySource/Meter
/// that has been explicitly added to the provider: <c>AddSource(...)</c> and
/// <c>AddMeter(...)</c> in <see cref="ObservabilityExtensions"/> reference these
/// same constants, so they cannot drift apart.
/// </summary>
public sealed class NomaosTelemetry : IDisposable
{
    public const string ActivitySourceName = "Agrovesto.NomaOS.Onboarding";
    public const string MeterName = "Agrovesto.NomaOS.Onboarding";

    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly Counter<long> _stepCounter;
    private readonly Histogram<double> _stepDuration;
    private bool _disposed;

    public NomaosTelemetry()
        : this(ActivitySourceName, MeterName)
    {
    }

    internal NomaosTelemetry(string activitySourceName, string meterName)
    {
        _activitySource = new ActivitySource(activitySourceName);
        _meter = new Meter(meterName);

        _stepCounter = _meter.CreateCounter<long>(
            name: "nomaos.onboarding.steps",
            unit: "{step}",
            description: "Onboarding steps executed, split by step name and outcome.");

        _stepDuration = _meter.CreateHistogram<double>(
            name: "nomaos.onboarding.step.duration",
            unit: "ms",
            description: "Wall-clock duration of an onboarding step in milliseconds.");
    }

    public ActivitySource ActivitySource => _activitySource;

    public Meter Meter => _meter;

    /// <summary>
    /// Starts a manual span. Returns null when nothing is listening (no
    /// exporter, or the source was not added to the provider) — that is the
    /// normal no-op path and callers must tolerate it, which is why every
    /// helper below is null-safe.
    /// </summary>
    public Activity? StartStep(string stepName, string? correlationId = null)
    {
        var activity = _activitySource.StartActivity($"onboarding.{stepName}", ActivityKind.Internal);
        activity?.SetTag("nomaos.step", stepName);

        if (activity is not null && !string.IsNullOrWhiteSpace(correlationId))
        {
            activity.SetTag("nomaos.correlation_id", correlationId);
        }

        return activity;
    }

    /// <summary>Records the custom counter + histogram for a completed step.</summary>
    public void RecordStep(string stepName, string outcome, double elapsedMilliseconds)
    {
        var tags = new TagList
        {
            { "nomaos.step", stepName },
            { "nomaos.outcome", outcome },
        };

        _stepCounter.Add(1, tags);
        _stepDuration.Record(elapsedMilliseconds, tags);
    }

    /// <summary>
    /// Runs <paramref name="work"/> inside a manual span and records the custom
    /// metrics for it. Exceptions are recorded on the span, the span status is
    /// set to Error, the "failure" outcome is counted, and the exception is
    /// rethrown — telemetry never swallows an error.
    /// </summary>
    public async Task<T> TrackAsync<T>(
        string stepName,
        Func<Activity?, CancellationToken, Task<T>> work,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);

        using var activity = StartStep(stepName);
        var stopwatch = Stopwatch.StartNew();
        var outcome = "success";

        try
        {
            var result = await work(activity, cancellationToken).ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            outcome = "failure";
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("exception.type", ex.GetType().FullName);
            activity?.AddEvent(new ActivityEvent("exception"));
            throw;
        }
        finally
        {
            stopwatch.Stop();
            RecordStep(stepName, outcome, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _activitySource.Dispose();
        _meter.Dispose();
    }
}
