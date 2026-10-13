using Xunit;

// Several suites in this assembly touch process-global state that cannot be
// isolated per test: Console.Out redirection (JSON log assertions),
// Activity.Current, and OpenTelemetry's Sdk.SetDefaultTextMapPropagator.
// Running collections in parallel would make those tests interfere with each
// other at random — the exact flakiness the coverage gate is supposed to
// prevent. The suite is fast (no I/O, no containers), so serialising it costs
// seconds and buys determinism.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
