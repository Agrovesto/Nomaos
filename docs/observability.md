# Observability — OpenTelemetry, Alloy, logs

Everything the API emits is OTLP or JSON on stdout. There is no vendor SDK in
the application, and no telemetry endpoint is hardcoded anywhere: the entire
configuration surface is the standard `OTEL_*` environment block, which Compose
supplies from `/opt/nomaos/.env.otel`.

```
                         ┌──────────────────────────┐
  traces  ──OTLP 4317────►                          ├──► Tempo
  metrics ──OTLP 4317────►      Grafana Alloy       ├──► Prometheus (remote_write)
  logs    ──OTLP 4317────►                          ├──► Loki
                         └─────────▲────────────────┘
  stdout JSON ──► docker json-file ─┘  (loki.source.docker)
```

Code lives in `Agrovesto.Backend.API/Observability/`:

| File | Responsibility |
|---|---|
| `ObservabilityOptions.cs` | The only reader of `OTEL_*`; turns env vars into a typed, testable object |
| `ObservabilityExtensions.cs` | SDK wiring: resource, propagators, instrumentation, exporters, demo endpoint |
| `NomaosTelemetry.cs` | The app's own `ActivitySource` + `Meter` and the custom instruments |
| `NomaosSerilog.cs` | JSON-to-stdout logging, one object per line |
| `ActivityTraceEnricher.cs` | Puts `trace_id`/`span_id` on every log event |
| `ObservabilityPaths.cs` | The probe/dashboard paths excluded from tracing |

---

## Configuration

| Variable | Default | Notes |
|---|---|---|
| `OTEL_SERVICE_NAME` | `nomaos-onboarding-api` | `service.name` |
| `OTEL_SERVICE_NAMESPACE` | `agrovesto` | |
| `OTEL_SERVICE_INSTANCE_ID` | container `HOSTNAME` | falls back to the machine name |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | *(unset)* | **Unset means "no collector": the exporters are not registered at all.** The SDK's default of `localhost:4317` would otherwise log a connection failure every few seconds forever |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` | `grpc` (4317) or `http/protobuf` (4318) |
| `OTEL_PROPAGATORS` | `tracecontext,baggage` | W3C only; unknown names are ignored and never produce an empty propagator |
| `OTEL_TRACES_SAMPLER` / `_ARG` | `parentbased_traceidratio` / `1.0` | read by the SDK itself |
| `OTEL_SDK_DISABLED` | `false` | kill switch — instruments still exist in-process, nothing is exported |
| `DEPLOYMENT_ENVIRONMENT` | `ASPNETCORE_ENVIRONMENT` | `deployment.environment` |
| `GIT_SHA` / `COMMIT_SHA` | `unknown` | `vcs.repository.ref.revision`, also on every log line and in `/status` |
| `LOG_LEVEL` | `Information` | Serilog minimum level |
| `LOG_FILE_PATH` | *(unset)* | optional rolling file, for hosts that also ship files |
| `AWS_LOGS_ENABLED` | `true` in Production | the legacy CloudWatch sink; set `false` once Loki is authoritative |

Full example: [`deploy/ec2/env.otel.example`](../deploy/ec2/env.otel.example).

---

## What is instrumented

* **ASP.NET Core** — server spans and `http.server.request.duration`.
* **HttpClient** — client spans for every outbound call (Termii, SendGrid,
  IdentityPass, Vault).
* **Microsoft.Data.SqlClient** — statement text is captured **sanitised**;
  parameter values are never recorded.
* **StackExchange.Redis** — only when an `IConnectionMultiplexer` is registered.
* **Runtime + process metrics** — GC, thread pool, exceptions, CPU, memory.
* **The app's own instruments** (`NomaosTelemetry`, meter/source
  `Agrovesto.NomaOS.Onboarding`): `nomaos.onboarding.steps` (Counter, tagged
  `nomaos.step` + `nomaos.outcome`) and `nomaos.onboarding.step.duration`
  (Histogram, ms). `TrackAsync` wraps a unit of work in a manual span and
  records both, marking the span `Error` and counting `outcome=failure` when the
  work throws — without swallowing the exception.

### Excluded from tracing

`/health`, `/health/live`, `/health/ready`, `/status`, `/hangfire` — for both
inbound requests and the app's own outbound probe calls. Docker, Compose and the
load balancer probe these several times a second; keeping them would make probe
spans the overwhelming majority of Tempo's ingest and would bury real traffic.

### Demo endpoint

```bash
curl -s localhost:8080/internal/telemetry/demo | jq
{
  "traceId": "0af7651916cd43dd8448eb211c80319c",
  "spanId": "b7ad6b7169203331",
  "recorded": ["nomaos.onboarding.steps", "nomaos.onboarding.step.duration"]
}
```

It creates a manual span with attributes and events, increments the counter and
records the histogram. Use it to prove the pipeline after any collector change:
the returned `traceId` should be searchable in Tempo within a few seconds.

---

## Logs

Serilog writes **one JSON object per line** to stdout (`JsonFormatter`), with
`writeToProviders: true` so `ILogger<T>` output from the framework and from
Hangfire flows through the same pipeline and also reaches the OTLP log exporter.

Every line carries:

```json
{
  "Timestamp": "2026-09-25T12:30:45.1234567Z",
  "Level": "Information",
  "MessageTemplate": "farmer {FarmerId} onboarded",
  "RenderedMessage": "farmer 4711 onboarded",
  "Properties": {
    "service.name": "nomaos-onboarding-api",
    "deployment.environment": "production",
    "git.sha": "abc1234",
    "trace_id": "0af7651916cd43dd8448eb211c80319c",
    "span_id": "b7ad6b7169203331"
  }
}
```

`trace_id`/`span_id` come from `Activity.Current`, which is what makes the
"logs for this trace" jump in Grafana work:

```logql
{app="nomaos-onboarding-api"} | json | trace_id="0af7651916cd43dd8448eb211c80319c"
```

The Compose `logging` block pins `json-file` with `max-size: 20m` and
`max-file: 5`. Rotation is not optional — an unrotated chatty release fills the
EBS volume and takes the database down with it.

---

## Alloy

[`deploy/ec2/alloy/config.alloy`](../deploy/ec2/alloy/config.alloy) receives OTLP
on `:4317` (gRPC) and `:4318` (HTTP), adds EC2 resource detection, batches, and
fans out to Tempo, Prometheus (`remote_write`) and Loki. Container logs are
discovered by the `com.nomaos.app` label, so a container that loses its labels
silently disappears from Loki — check the labels first when logs stop arriving.

Run it as a sidecar with `docker compose --profile alloy up -d`, or prefer a
single host-level Alloy when the box runs several services.
