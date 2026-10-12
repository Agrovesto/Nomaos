using System.Diagnostics;
using Agrovesto.Backend.API.Observability;
using FluentAssertions;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace NomaOS.Test.Observability;

/// <summary>
/// The Loki -> Tempo jump in Grafana is configured on the <c>trace_id</c> field
/// produced here, so these assertions are effectively the contract for that
/// dashboard link.
/// </summary>
public class ActivityTraceEnricherTests
{
    private sealed class PropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false) =>
            new(name, new ScalarValue(value));
    }

    private static LogEvent NewLogEvent() =>
        new(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            exception: null,
            new MessageTemplateParser().Parse("farmer {FarmerId} onboarded"),
            Array.Empty<LogEventProperty>());

    [Fact]
    public void Enrich_AddsTraceAndSpanIdsFromTheAmbientActivity()
    {
        using var activity = new Activity("test-span");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var logEvent = NewLogEvent();
        new ActivityTraceEnricher(() => activity).Enrich(logEvent, new PropertyFactory());

        logEvent.Properties.Should().ContainKey("trace_id");
        logEvent.Properties["trace_id"].ToString().Trim('"').Should().Be(activity.TraceId.ToHexString());
        logEvent.Properties["span_id"].ToString().Trim('"').Should().Be(activity.SpanId.ToHexString());
        logEvent.Properties.Should().NotContainKey("trace_state");
    }

    [Fact]
    public void Enrich_IncludesTraceStateWhenPresent()
    {
        using var activity = new Activity("test-span");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.TraceStateString = "congo=t61rcWkgMzE";
        activity.Start();

        var logEvent = NewLogEvent();
        new ActivityTraceEnricher(() => activity).Enrich(logEvent, new PropertyFactory());

        logEvent.Properties["trace_state"].ToString().Trim('"').Should().Be("congo=t61rcWkgMzE");
    }

    [Fact]
    public void Enrich_IsANoOpWithoutAnActivity()
    {
        var logEvent = NewLogEvent();

        new ActivityTraceEnricher(() => null).Enrich(logEvent, new PropertyFactory());

        logEvent.Properties.Should().BeEmpty();
    }

    [Fact]
    public void Enrich_DoesNotOverwriteAnExistingProperty()
    {
        using var activity = new Activity("test-span");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var logEvent = NewLogEvent();
        logEvent.AddPropertyIfAbsent(new LogEventProperty("trace_id", new ScalarValue("pre-existing")));

        new ActivityTraceEnricher(() => activity).Enrich(logEvent, new PropertyFactory());

        logEvent.Properties["trace_id"].ToString().Trim('"').Should().Be("pre-existing");
    }

    [Fact]
    public void DefaultConstructor_ReadsActivityCurrent()
    {
        using var activity = new Activity("ambient");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var logEvent = NewLogEvent();
        new ActivityTraceEnricher().Enrich(logEvent, new PropertyFactory());

        logEvent.Properties["trace_id"].ToString().Trim('"').Should().Be(Activity.Current!.TraceId.ToHexString());
    }

    [Fact]
    public void Enrich_ValidatesItsArguments()
    {
        var enricher = new ActivityTraceEnricher(() => null);

        var nullEvent = () => enricher.Enrich(null!, new PropertyFactory());
        var nullFactory = () => enricher.Enrich(NewLogEvent(), null!);

        nullEvent.Should().Throw<ArgumentNullException>();
        nullFactory.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_RejectsANullAccessor()
    {
        var act = () => new ActivityTraceEnricher(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
