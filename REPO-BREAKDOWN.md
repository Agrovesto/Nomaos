# NomaOS — repository breakdown

**Audited at `963d8fa` on `dev` (2026-10-14).** This supersedes the edition written
against `f839e01` on `master`; the differences are in §10, and they matter — the
headline findings of that edition (no telemetry, no tiered health checks) had
already been fixed on `dev`.

## 0. How to read the numbers

Every figure here was produced by a command in this file, so a change in the repo
can be re-checked rather than re-trusted:

| Quantity | Command |
| --- | --- |
| Hand-written lines | `find <proj> -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' -not -path '*/Migrations/*' -exec cat {} + \| grep -vE '^\s*(//\|\///\|\*\|/\*)' \| grep -vc '^\s*$'` |
| Routes | `grep -rcE '\[Http(Get\|Post\|Put\|Delete\|Patch)' Agrovesto.Backend.API/Controllers` |
| Anonymous endpoints | `grep -rc '\[AllowAnonymous\]' --include=*.cs Agrovesto.Backend.API` |
| Test methods | `grep -rcE '^\s*\[(Fact\|Theory)\]' NomaOS.Test --include=*.cs` |
| DbSets | `grep -cE 'public DbSet' Agrovesto.Backend.Infrastructure/DB/DataContext.cs` |

Migrations are excluded from line counts on purpose: 381 generated `.cs` files: the API project
is 911,466 raw lines, of which **889,006 are `Migrations/`** (22,460 is everything
else). Counting them would swamp every ratio in this doc.

## 1. What it is

Backend API for **Agrovesto**, a multi-tenant agri-fintech lender in Nigeria: an
organization (cooperative, aggregators) onboards farmers and agents, KYC and
geospatial verification gate a season of farm inputs bought on credit, inputs are
disbursed from a warehouse, and repayments flow back through a wallet with
penalties and a credit score on top.

`organization → farmer/agent → KYC (Prembly/IdentityPass + Rekognition) → GIS polygon → input bundle on credit (effective annualised pricing) → warehouse disbursement → repayment schedule → wallet collection → penalties + credit score → M&E and receipts`

Nothing in this repo renders a page. It is the API plus the background jobs behind
`NomaOS`, and a separate admin/mobile client consumes it.

## 2. Solution and projects

| Project | Files (hand) | Hand-written lines | Role |
| --- | --- | --- | --- |
| `Agrovesto.Backend.API` | 70 | 18,126 | Host, controllers, middleware, health, observability, migrations |
| `Agrovesto.Backend.Core` | 147 | 10,142 | Entities, enums, interfaces, DTOs — no EF, no ASP.NET |
| `Agrovesto.Backend.Infrastructure` | 243 | 59,475 | EF `DataContext`, managers, engines, external providers, wallet/penalty logic |
| `NomaOS.Test` | 42 | 7,328 | The test project CI actually runs (356 `[Fact]`/`[Theory]`) |
| `Agrovesto.Backend.Tests` | 1 | 33 | **A stub**: one `UnitTest1.cs`, not referenced by CI |
| | **95,104** | | |

Two structural notes that cost time if learned the hard way:

* The dependency direction is `API → Infrastructure → Core`. Anything a manager in
  `Infrastructure` must call (including telemetry) has to live in Core or
  Infrastructure, not in the API project.
* `dotnet test` in CI is pinned to `NomaOS.Test/NomaOS.Test.csproj`
  (`.github/workflows/dev.yml:27`). Adding a test to `Agrovesto.Backend.Tests`
  produces a green build that verifies nothing.

## 3. HTTP surface

522 route attributes across 47 controller files: **228 on the V1-era controllers**
directly in `Controllers/`, **294 in `Controllers/V2/Internal|Public/`** (18
files). Largest: `V2/Internal/OrganizationController` 85, `V2/Internal/InputFinancingController`
42, `V2/Public/MandEController` 33, `V2/Internal/AdminController` 24 — the V2 tree
splits `Internal` (15 files) from `Public` (2), with `BankController` at the V2 root. Three controller files are
stubs (≤6 hand-written lines).

`/health`, `/health/live`, `/health/ready`, `/status` and `/swagger` are mapped in
`Program.cs`/`Health/`, not by a controller — `Controllers/HealthController.cs` is a
legacy duplicate that still exists (§10).

## 4. Data layer

`DataContext` (`Agrovesto.Backend.Infrastructure/DB/DataContext.cs`) exposes
**140 `DbSet`s**. EF migrations live in `Agrovesto.Backend.API/Migrations`
(`sqlOptions.MigrationsAssembly("Agrovesto.Backend.API")`, `Program.cs:147`) with a
continuous history from `20250123193815_initial db creation` to
`20261012083007_added farmer status`.

SQL Server options worth knowing (`Program.cs:145-156`): `EnableRetryOnFailure`
5 retries / 10 s max delay, `CommandTimeout(300)`, `UseNetTopologySuite()` — the
last one is why farm polygons are queryable spatially rather than as GeoJSON blobs.

### Engines (the parts worth reading before changing)

| File (`Infrastructure/Logic/Services/`) | What it decides |
| --- | --- |
| `RepaymentEngine.cs` | Builds the schedule. `AddFrequency` maps `Daily → AddDays(7)`, `Weekly → AddDays(14)` — asserted by `NomaOS.Test/RepaymentEnginesTests.cs`, so it is deliberate, but the enum names do not mean what an accountant reads. |
| `RepaymentAllocationEngine.cs` | Waterfall: penalty → interest → principal. |
| `PenaltyEngine.cs` | Grace period, then flat fee + `Amount × DailyInterestRate × daysPastDue`, capped by `MaxPenaltyCap`. |
| `Logic/Services/MandE/IMandEFormulaEngine.cs` | Monitoring & evaluation formulas. |
| `Utilities/GeometryHelper.cs` | Haversine distance, shoelace area → hectares, `AllowedRadiusMeters` polygon check. |

None of these five changed between `f839e01` and `963d8fa`
(`git diff --quiet f839e01 963d8fa -- <path>` for each). The 2,652 commits on `dev`
are overwhelmingly deploy/CI/observability work plus entities, not money logic.

## 5. AuthN / AuthZ

One scheme: `AddAuthentication(o => o.DefaultAuthenticateScheme =
JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` (`Program.cs:239-244`)
over ASP.NET Identity (`AddIdentity<User, Role>`, `Program.cs:201`). **No cookie
scheme exists**, which is the constraint behind the `/hangfire` fix in §10.

`[AllowAnonymous]` appears **39 times** in the API project. Distribution:
`LOVController` 10 (list-of-values lookups — reasonable),
`V2/Internal/AuthenticationController` 9 (login/OTP/refresh — required),
`V2/Internal/OrganizationController` **8** (organization writes — not obviously
required), `BusinessController` 4, `V2/Internal/ProfileController` 2,
`V2/Internal/AdminController` 2, four files with 1 each. Down from 49 on the
`master` edition; the reduction was in `OrganizationController` (12 → 8) and the
V1-era controllers.

Pipeline order (`Program.cs:480-558`, and the part this repo reorders):
`UseForwardedHeaders → CorrelationIdMiddleware → (dev) Swagger → GlobalExceptionMiddleware → UseCors → UseRouting → UseRateLimiter → MapNomaosHealthEndpoints → MapObservabilityDemoEndpoint → UseHttpsRedirection → UseStaticFiles → UseAuthentication → UseAuthorization → UseHangfireDashboard → MapControllers`.

## 6. Observability — what `dev` already has

This is the section where the `master` edition was most wrong. On `dev`:

* **Logging.** Serilog, JSON one object per line on stdout, `writeToProviders: true`
  so the MEL/OTLP path sees the same events (`Program.cs:63-69`, `Observability/NomaosSerilog.cs`).
  `LOG_LEVEL`, `LOG_FILE_PATH` (opt-in rolling file, 7 retained), minimum-level
  overrides that mute `Microsoft.*`, EF Core and `System.Net.Http.HttpClient` to
  Warning. Enrichers: `FromLogContext`, `ActivityTraceEnricher`
  (`trace_id`/`span_id` in snake_case so Loki's derived field can deep-link into
  Tempo), plus `service.name`, `deployment.environment`, `git.sha`.
* **Traces + metrics.** `Agrovesto.Backend.API/Observability/` (6 files on `dev`, 8 after this PR):
  `ObservabilityOptions` (the only reader of the `OTEL_*` env block),
  `ObservabilityExtensions` (`AddSource` + AspNetCore/HttpClient/SqlClient +
  conditional Redis instrumentation, metrics with runtime + process, OTLP
  exporter registered **only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set**),
  `ObservabilityPaths` (`/health`, `/status`, `/hangfire` filtered before span
  creation, unit-tested), `NomaosTelemetry` (`ActivitySource` + `Meter` singleton
  with `nomaos.onboarding.steps` counter and `…step.duration` histogram, and
  `TrackAsync`, which rethrows after recording).
  SQL statement capture is left off by design: `CommandText` carries BVN/NIN and
  farmer PII in this codebase.
* **Health.** Four surfaces with an explicit contract table
  (`Health/HealthCheckExtensions.cs:12-17`): `/health/live` (self), `/health/ready`
  (SQL/Redis/Vault), `/health` (both, back-compat), `/status` (report, **always
  200**). `ResultStatusCodes[Degraded] = 200` on the gating endpoints
  (`HealthCheckExtensions.cs:145`), no-store headers on probes, and `RedisReadinessHealthCheck`
  / `VaultReadinessHealthCheck` report `Degraded` rather than failing when the
  dependency is not configured.
* **Build/deploy hygiene.** `Dockerfile` has `HEALTHCHECK` (`:135`), `USER $APP_UID`
  (`:143`) and an array `ENTRYPOINT` (`:148`).

Correlation id ships with this doc's PR; note it is also added to `Access-Control-Expose-Headers` on the default CORS policy, without which a browser cannot read the response header at all.

Gaps that remain after this PR's changes: no OTLP collector anywhere in `deploy/`,
`ObservabilityOptions`' doc comment pointing at a file that does not exist
(`deploy/ec2/env.otel.example`), no explicit histogram buckets anywhere
(`grep -n 'AddView\|ExplicitBucket' Agrovesto.Backend.API/Observability/*.cs` →
empty), and `MapObservabilityDemoEndpoint` is `.AllowAnonymous()` in every
environment (`ObservabilityExtensions.cs:170-171`).

## 7. Config, secrets, cache, jobs

* **No base `appsettings.json`.** Only `appsettings.Development.json`,
  `.Staging.json`, `.Production.json` are committed; per-environment files plus the
  environment plus Vault.
* **Vault is fail-fast in non-Development**: `VAULT_ADDR`, `VAULT_ROLE_ID`,
  `VAULT_SECRET_ID`, `VAULT_SECRET_PATH` are each checked and `throw new
  InvalidOperationException` when missing (`Program.cs:79-110`), with
  `reloadOnChange: true` / 60 s polling, and the Vault provider registered *after*
  the env provider so Vault wins for `ConnectionStrings:DefaultConnection`.
* **Redis** via `AddStackExchangeRedisCache(options => options.Configuration =
  Configuration["Redis:ConnectionString"])` (`Program.cs:140-143`); compose maps
  `Redis__ConnectionString: "${REDIS_CONNECTION_STRING:-redis:6379}"`
  (`deploy/compose/docker-compose.app.yml:64`). A Redis outage throws on cache use:
  there is no try/catch wrapper, so the dependency is fail-**closed** in the app and
  fail-open only in the health report (`Degraded`).
* **Hangfire**: storage configured only when a DB exists
  (`hangfireEnabled = databaseConfigured && HANGFIRE_ENABLED != "false"`,
  `Program.cs:163-164`). Zero `RecurringJob` registrations; four files enqueue work
  (`BulkOnboardinProcessor`, `InputFinancingManager`, `OnBoardingManager`,
  `OrganizationManager`). `databaseConfigured` also gates the CI container smoke
  test so the image boots without a DB and still answers `/health/live`.

## 8. Delivery

`deploy/` = `Makefile`, `README.md`, `compose/` (3 files: app, infra, migrate),
`env/` (`app.env.example`, `app.json.example`, `infra.env.example`), `nginx/`,
`scripts/` (`deploy.sh` ~900 lines, `lib.sh`) — blue/green through nginx upstream
swap, `STOP_GRACE_PERIOD`, `HEALTHCHECK_START_PERIOD`.

`.github/workflows/` on `dev` has **5** files (`dev`, `docker-deploy`, `ecs`,
`prod`, `staging`); `master` registers a different set. `dev.yml` jobs:
`secret-and-fs-scan` (TruffleHog), `iac-scan` (Checkov, `soft_fail: false`),
`dependency-check` (NuGet vulnerability scan), `sonarqube-sast` (restore → build
Release → `dotnet test NomaOS.Test` with OpenCover + TRX into Sonar,
`sonar.coverage.exclusions=**/*Tests/**,**/Program.cs,**/Startup.cs`),
`validate-image` (build + scan, no push), `build-and-push`
(`if: github.event_name == 'push' || workflow_dispatch`, so a PR cannot deploy).
Coverage policy is scoped-and-real: `NomaOS.Test/coverlet.runsettings` measures
`Agrovesto.Backend.API.Observability.*` and `…Health.*` with `<Threshold>95</Threshold>`,
and says why in its header comment — the legacy surface is excluded so the gate
stays meaningful. The repo-root `coverlet.runsettings` produces the wide report for
Sonar and is deliberately left loose. There is no whole-repository ratchet.

## 9. Tests

356 `[Fact]`/`[Theory]` in 42 files under `NomaOS.Test`, xunit + FluentAssertions +
Moq + Testcontainers, `Microsoft.AspNetCore.Mvc.Testing` available. Coverage-carrying
groups: `Health/` (8 files — response writers, payload factory, Redis/Vault/Self
checks, integration tests) and `Observability/` (6 on `dev`, 8 after this PR —
telemetry, extensions, options, paths, enricher, Serilog), plus the wallet/repayment/withdrawal/PIN/OTP
suites and `TestSupport/StubHttp.cs` for provider fakes. Style worth matching:
assertions through `ActivityListener`/`MeterListener` rather than mocks, so a test
proves an instrument actually emitted.

## 10. Findings, ranked

| # | Finding | State on `dev` today |
| --- | --- | --- |
| 1 | `/hangfire` was mapped bare: job list + arguments (phone numbers, wallet refs, KYC payloads) and retry/trigger/delete buttons, unauthenticated. | **Fixed in this PR** — role or edge-token allow-list, and moved after `UseAuthentication`/`UseAuthorization`. |
| 2 | No client-facing correlation id: `trace_id` is on every log line, but nothing returns an id a support agent can collect, and none exists for paths deliberately not traced or when `OTEL_SDK_DISABLED=true`. | **Fixed in this PR** — `CorrelationIdMiddleware`. |
| 3 | No metric for money moved: Prometheus can see step counts and durations, but not disbursement or repayment volume without a SQL query. | Instrument added (`nomaos.money.amount`, kobo, `Histogram<long>`). **Call sites deliberately not wired**: `NomaosTelemetry` lives in the API project and the engines live in `Infrastructure`, so recording a repayment means deciding where the seam goes. That decision is the follow-up. |
| 4 | `V2/Internal/OrganizationController` has 8 `[AllowAnonymous]` write endpoints. | Open. Needs the owning team to confirm whether the admin panel calls them without a token. |
| 5 | No OTLP collector in `deploy/compose`, though `ObservabilityOptions` and `NomaosSerilog` describe Alloy/Loki/Tempo as the destination and point at `deploy/ec2/env.otel.example`, which does not exist. Locally, exporters stay off (correct) and the observability stack is undeployable from this repo. | Open. Add an `alloy` service to `docker-compose.infra.yml` (or a `docker-compose.otel.yml` overlay) + fix the stale path in that comment. |
| 6 | No explicit histogram buckets: `nomaos.onboarding.step.duration` (ms) and `nomaos.money.amount` (kobo) get the SDK's default boundaries, which are wrong for both — most steps are single-digit ms, kobo amounts span 4 orders of magnitude. | Open. One `AddView(name, new ExplicitBucketBoundariesView(...))` per instrument, in `WithMetrics`. |
| 7 | `GET /internal/telemetry/demo` is `AllowAnonymous()` in production. Payload is only trace/span ids, but it is an unauthenticated endpoint on a payments API. | Open. Gate it the way `/hangfire` now is, or map it only in Development. |
| 8 | `Redis` cache has no failure containment: an outage turns cache hits into 500s rather than degraded reads. | Open. Decide explicitly — either wrap the two `IDistributedCache` uses or document fail-closed as policy. |
| 9 | `Controllers/HealthController.cs` duplicates the `/health` surface now owned by `Health/HealthCheckExtensions.cs`. | Open. Delete or redirect once nothing routes to it. |
| 10 | `RepaymentEngine.AddFrequency`: `Daily → AddDays(7)`, `Weekly → AddDays(14)`. Test-asserted, so a "fix" here changes schedules on live loans. | Open by choice. Rename the enum members (`Daily`/`Weekly` → whatever the interval actually is) rather than changing arithmetic in an infra PR. |
| 11 | `Agrovesto.Backend.Tests` is a 1-test stub in the solution while CI runs only `NomaOS.Test`. | Open. Delete the project, or point CI at both. |
| 12 | Outbound HTTP is raw `new HttpClient(...)` across the external providers, not `IHttpClientFactory`: OTel still traces it (DiagnosticSource-based), but sockets/ports are churned and there is no per-client policy seam. | Open, large. |
| 13 | 9 V1 controller files with no routes were reported by the `master` edition; 3 stubs remain. | Mostly closed. |

## 11. What the `master`-snapshot edition of this doc got wrong

Useful to know if the earlier text is still in circulation (PR #415), because the
errors were all in the same direction — this repo's operational maturity was
understated:

| Claim against `f839e01` | Reality on `963d8fa` |
| --- | --- |
| "0 OpenTelemetry packages; no traces obtainable" | Full OTLP traces/metrics/logs wiring in `API/Observability/` (6 files) + 6 test files |
| "No tiered health checks" | 4 surfaces, `Degraded → 200`, tag-based `live`/`ready`, no-store headers |
| "Logs are not structured for Loki" | JSON stdout + `trace_id`/`span_id` enricher + `writeToProviders` |
| "500 endpoints: 226 V1 + 274 V2" | 522: 228 + 294 |
| "49 `[AllowAnonymous]`" | 39 (`OrganizationController` 12 → 8) |
| "9 empty V1 stubs, 12,165 Core / 76,250 Infrastructure hand-written lines" | 3 stubs; 10,142 / 59,475 with migrations excluded |
| "migrations restart at `20260625`" | 381 files, continuous from `20250123193815_initial db creation` |
| "Program.cs is 477 lines" | 560 |
| "master is what's deployed and dev lags behind it" | Backwards: `dev` contains 2,652 commits `master` lacks; `master` has exactly 1 commit `dev` lacks (hotfix #412, `f839e01`) |

**The transferable lesson for this repo:** audit `dev` (or the branch a service
actually deploys from) before treating "missing X" as a finding. `master` here is a
release branch that lags integration by ~2.6 k commits, which is long enough for
"this service has no telemetry" to be simultaneously true and useless.
