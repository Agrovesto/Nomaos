using System.Diagnostics;
using System.Diagnostics.Metrics;
using Agrovesto.Backend.API.Observability;
using FluentAssertions;

namespace NomaOS.Test.Observability;

/// <summary>
/// Exercises the manual-span + custom-metric path with real
/// <see cref="ActivityListener"/> / <see cref="MeterListener"/> plumbing — the
/// same mechanism the OpenTelemetry SDK uses — so the assertions prove the
/// instruments actually emit, not merely that the methods return.
/// </summary>
public class NomaosTelemetryTests : IDisposable
{
    private readonly string _uniqueName = "NomaosTelemetryTests." + Guid.NewGuid().ToString("N");
    private readonly NomaosTelemetry _telemetry;
    private readonly ActivityListener _activityListener;
    private readonly List<Activity> _startedActivities = new();
    private readonly List<Activity> _stoppedActivities = new();

    public NomaosTelemetryTests()
    {
        _telemetry = new NomaosTelemetry(_uniqueName, _uniqueName);

        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == _uniqueName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _startedActivities.Add(activity),
            ActivityStopped = activity => _stoppedActivities.Add(activity),
        };

        ActivitySource.AddActivityListener(_activityListener);
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _telemetry.Dispose();
        GC.SuppressFinalize(this);
    }

    private (List<(string Instrument, long Value, string Outcome)> Counters,
             List<(string Instrument, double Value, string Outcome)> Histograms) Collect(Action action)
    {
        var counters = new List<(string, long, string)>();
        var histograms = new List<(string, double, string)>();

        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == _uniqueName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };

        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            counters.Add((instrument.Name, measurement, OutcomeOf(tags))));

        meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
            histograms.Add((instrument.Name, measurement, OutcomeOf(tags))));

        meterListener.Start();
        action();
        meterListener.RecordObservableInstruments();

        return (counters, histograms);

        static string OutcomeOf(ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "nomaos.outcome")
                {
                    return tag.Value?.ToString() ?? string.Empty;
                }
            }

            return string.Empty;
        }
    }

    [Fact]
    public void DefaultConstructor_UsesTheConstantsTheSdkSubscribesTo()
    {
        using var telemetry = new NomaosTelemetry();

        telemetry.ActivitySource.Name.Should().Be(NomaosTelemetry.ActivitySourceName);
        telemetry.Meter.Name.Should().Be(NomaosTelemetry.MeterName);
    }

    [Fact]
    public void StartStep_CreatesANamedSpanWithTheStepTag()
    {
        using var activity = _telemetry.StartStep("wallet-provisioning", "corr-42");

        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("onboarding.wallet-provisioning");
        activity.GetTagItem("nomaos.step").Should().Be("wallet-provisioning");
        activity.GetTagItem("nomaos.correlation_id").Should().Be("corr-42");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StartStep_OmitsAnEmptyCorrelationId(string? correlationId)
    {
        using var activity = _telemetry.StartStep("step", correlationId);

        activity!.GetTagItem("nomaos.correlation_id").Should().BeNull();
    }

    [Fact]
    public void StartStep_ReturnsNullWhenNobodyIsListening()
    {
        using var unlistened = new NomaosTelemetry("unlistened." + Guid.NewGuid().ToString("N"), "unlistened");

        unlistened.StartStep("step").Should().BeNull();
    }

    [Fact]
    public void RecordStep_EmitsBothInstruments()
    {
        var (counters, histograms) = Collect(() => _telemetry.RecordStep("kyc", "success", 12.5));

        counters.Should().ContainSingle()
            .Which.Should().Be(("nomaos.onboarding.steps", 1L, "success"));

        histograms.Should().ContainSingle()
            .Which.Should().Be(("nomaos.onboarding.step.duration", 12.5, "success"));
    }

    [Fact]
    public void RecordMoney_EmitsAnIntegerHistogramWithOperationAndOutcome()
    {
        var (counters, histograms) = Collect(() => _telemetry.RecordMoney("disbursement", "success", 250_000));

        // A long measurement lands in the helper's "counters" list because the
        // callback is registered per measurement *type*, not per instrument kind
        // — which is also why money is a long: NGN subunits must not round.
        counters.Should().ContainSingle()
            .Which.Should().Be(("nomaos.money.amount", 250_000L, "success"));
        histograms.Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-250_000)]
    public void RecordMoney_RelabelsANegativeAmountInsteadOfThrowingOrDroppingIt(long amountKobo)
    {
        // Telemetry on a payment path must never fail the payment, and it must
        // never silently vanish either: zero under "invalid" keeps the event
        // visible in Grafana and greppable as a call-site bug.
        var (counters, _) = Collect(() => _telemetry.RecordMoney("refund", "failure", amountKobo));

        counters.Should().ContainSingle()
            .Which.Should().Be(("nomaos.money.amount", 0L, "invalid"));
    }

    [Fact]
    public void RecordMoney_DefaultsBlankDimensionsToUnknown()
    {
        string? operation = null;
        string? outcome = null;

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == _uniqueName && instrument.Name == "nomaos.money.amount")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            },
        };

        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "nomaos.money.operation")
                {
                    operation = tag.Value?.ToString();
                }

                if (tag.Key == "nomaos.outcome")
                {
                    outcome = tag.Value?.ToString();
                }
            }
        });

        listener.Start();
        _telemetry.RecordMoney("  ", string.Empty, 500);

        // Two blanks would otherwise produce an unlabelled series nobody can
        // filter on.
        operation.Should().Be("unknown");
        outcome.Should().Be("unknown");
    }

    [Fact]
    public void TrackAsync_RecordsSuccessAndReturnsTheResult()
    {
        var result = 0;

        var (counters, histograms) = Collect(() =>
        {
            result = _telemetry.TrackAsync(
                "onboard",
                async (activity, _) =>
                {
                    activity.Should().NotBeNull();
                    await Task.Yield();
                    return 42;
                }).GetAwaiter().GetResult();
        });

        result.Should().Be(42);
        counters.Should().ContainSingle().Which.Outcome.Should().Be("success");
        histograms.Should().ContainSingle();
        _stoppedActivities.Should().ContainSingle(activity => activity.OperationName == "onboarding.onboard");
        _stoppedActivities.Single(activity => activity.OperationName == "onboarding.onboard")
            .Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Fact]
    public void TrackAsync_RecordsFailureRethrowsAndMarksTheSpan()
    {
        var boom = new InvalidOperationException("wallet provider rejected the request");

        var (counters, histograms) = Collect(() =>
        {
            var act = () => _telemetry
                .TrackAsync<int>("onboard", (_, _) => throw boom)
                .GetAwaiter()
                .GetResult();

            act.Should().Throw<InvalidOperationException>().WithMessage("wallet provider rejected the request");
        });

        counters.Should().ContainSingle().Which.Outcome.Should().Be("failure");
        histograms.Should().ContainSingle().Which.Outcome.Should().Be("failure");

        var span = _stoppedActivities.Single(activity => activity.OperationName == "onboarding.onboard");
        span.Status.Should().Be(ActivityStatusCode.Error);
        span.StatusDescription.Should().Be("wallet provider rejected the request");
        span.GetTagItem("exception.type").Should().Be(typeof(InvalidOperationException).FullName);
        span.Events.Should().Contain(e => e.Name == "exception");
    }

    [Fact]
    public async Task TrackAsync_RejectsANullWorkDelegate()
    {
        var act = async () => await _telemetry.TrackAsync<int>("step", null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var telemetry = new NomaosTelemetry("dispose." + Guid.NewGuid().ToString("N"), "dispose");

        telemetry.Dispose();
        var act = telemetry.Dispose;

        act.Should().NotThrow();
    }
}
