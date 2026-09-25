using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Agrovesto.Backend.API.Observability;

/// <summary>
/// Stamps every Serilog event with the active W3C trace/span id.
///
/// This is what makes "jump from a Loki log line to the Tempo trace" work in
/// Grafana: Loki's derived field is configured on <c>trace_id</c>, so the field
/// name must stay snake_case and must match the trace id format Tempo indexes
/// (32 lowercase hex chars, i.e. <see cref="ActivityTraceId.ToHexString"/>).
///
/// Cheap by design — a null Activity short-circuits, and no allocation happens
/// when there is no ambient span.
/// </summary>
public sealed class ActivityTraceEnricher : ILogEventEnricher
{
    private readonly Func<Activity?> _activityAccessor;

    public ActivityTraceEnricher()
        : this(static () => Activity.Current)
    {
    }

    internal ActivityTraceEnricher(Func<Activity?> activityAccessor)
    {
        _activityAccessor = activityAccessor ?? throw new ArgumentNullException(nameof(activityAccessor));
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        var activity = _activityAccessor();
        if (activity is null)
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("trace_id", activity.TraceId.ToHexString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("span_id", activity.SpanId.ToHexString()));

        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("trace_state", activity.TraceStateString));
        }
    }
}
