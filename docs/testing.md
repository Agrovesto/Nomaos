# Testing & the coverage gate

```bash
./NomaOS.Test/run-tests-with-coverage.sh              # tests + 95% gate
./NomaOS.Test/run-tests-with-coverage.sh --threshold 90
./NomaOS.Test/run-tests-with-coverage.sh --filter 'FullyQualifiedName~Health'
```

Stack: xUnit + Moq + FluentAssertions + Coverlet, in the existing `NomaOS.Test`
project.

## The gate

`NomaOS.Test/coverlet.runsettings` measures **only the modules this work owns**:

```
[Agrovesto.Backend.API]Agrovesto.Backend.API.Observability.*
[Agrovesto.Backend.API]Agrovesto.Backend.API.Health.*
```

Measuring the whole legacy API here would drown the threshold in pre-existing
untested controllers and make the number meaningless. The repository-root
`coverlet.runsettings` still produces the wide OpenCover report that the
existing `dev.yml` workflow consumes and is untouched.

The threshold is **95% line AND 95% branch**, enforced by
`run-tests-with-coverage.sh`, which parses the Cobertura report and exits 1 when
either is below the bar (`COVERAGE_THRESHOLD` overrides it). Critical paths —
the health response writers, the status/readiness mapping, configuration
validation — are covered exhaustively rather than to a percentage.

Reports land in `NomaOS.Test/TestResults/coverage/`:

| File | Consumer |
|---|---|
| `coverage.cobertura.xml` | the gate, CI artifacts |
| `coverage.opencover.xml` | SonarQube (`sonar.cs.opencover.reportsPaths`) |
| `coverage.info` | LCOV, editors / Codecov |
| `coverage-summary.md` | appended to the GitHub Actions job summary |

## Zero flakiness by construction

No test touches a real SQL Server, Redis, Vault, socket or clock.

* **HTTP** — `StubHttpMessageHandler` / `StubHttpClientFactory`
  (`NomaOS.Test/TestSupport/`). Nothing ever resolves a hostname.
* **Redis** — `StubDistributedCache`, which is the same `IDistributedCache`
  abstraction the production readiness check uses, with a `FailWith` switch for
  the failure branch.
* **Time** — `HealthResponseWriters.Clock` and `SelfLivenessHealthCheck`'s clock
  are injectable, so timestamps and uptime are asserted exactly instead of
  "approximately now".
* **HTTP pipeline** — the endpoint matrix runs on `HostBuilder` +
  `UseTestServer()` + `GetTestClient()`, not on `WebApplicationFactory<Program>`:
  the real `Program.cs` wants a database, a Vault and an OTLP collector, and
  booting it in tests would be the single biggest source of flakiness.
* **Parallelism** — disabled for the assembly (`NomaOS.Test/AssemblyInfo.cs`).
  Several suites redirect `Console.Out`, set `Activity.Current` or install a
  global propagator; running collections concurrently would let them interfere
  at random. The suite is I/O-free and fast, so serialising it costs seconds.
* **Exporters** — a test never builds an OTLP-enabled `TracerProvider`. The
  exporter-configured path is asserted at *registration* (the `WithTracing`
  callback runs immediately); providers are only materialised in the
  no-endpoint case. Otherwise every CI run would sit behind a 10-second export
  timeout to a collector that does not exist.
* **AWS** — the CloudWatch Serilog sink is behind an `[ExcludeFromCodeCoverage]`
  helper. Constructing it starts a background flush loop against AWS; the
  *decision* to enable it (`NomaosSerilog.CloudWatchEnabled`) is fully tested,
  the SDK call itself is not.

## What is covered

| Area | File |
|---|---|
| `OTEL_*` parsing, propagator list, version resolution | `Observability/ObservabilityOptionsTests.cs` |
| Excluded probe paths | `Observability/ObservabilityPathsTests.cs` |
| ActivitySource/Meter, counter + histogram, `TrackAsync` success & failure | `Observability/NomaosTelemetryTests.cs` |
| Log enrichment with `trace_id`/`span_id` | `Observability/ActivityTraceEnricherTests.cs` |
| JSON-per-line stdout, level, file sink, CloudWatch decision | `Observability/NomaosSerilogTests.cs` |
| SDK wiring, resource attributes, request filters, demo handler | `Observability/ObservabilityExtensionsTests.cs` |
| Build identity, uptime, memory snapshot | `Health/BuildInfoTests.cs` |
| Payload shaping, readiness rules, redaction | `Health/HealthPayloadFactoryTests.cs` |
| Response writers (bytes on the wire, headers, DI fallback) | `Health/HealthResponseWritersTests.cs` |
| The three probes, every status branch | `Health/{SelfLiveness,RedisReadiness,VaultReadiness}HealthCheckTests.cs` |
| End-to-end status-code matrix over TestServer | `Health/HealthEndpointsIntegrationTests.cs` |

## In CI

The `dotnet-api` pipeline (see [ci-cd.md](./ci-cd.md)) → job
**Tests & coverage gate** runs the same script, uploads `coverage/` and the `.trx`, and fails the build on a test
failure or on coverage below 95%. The SonarQube job consumes the OpenCover
report from that artifact.
