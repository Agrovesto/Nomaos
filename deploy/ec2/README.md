# NomaOS Onboarding API — EC2 + Docker Compose deployment

The deployment model in one line:

```
GitHub Actions builds the image → pushes it to ECR → the EC2 host pulls it →
docker compose up -d → health gate on /health/live then /health/ready
```

No ECS, no Fargate, no task definitions. The host is the orchestrator, Compose
is the unit of deployment, and every release is an immutable `sha-<short_sha>`
image tag that can be rolled back by name.

> The older blue/green tooling in `deploy/scripts/` (nginx upstream flip, two
> slots) is independent of this directory and still works. This directory is the
> single-slot, pull-based flow used by `.github/workflows/dotnet-api.yml`.

---

## 1. Files

| File | Purpose |
|---|---|
| `docker-compose.yml` | The `api` service (ECR image), plus optional `redis` (profile `dev`) and `alloy` (profile `alloy`) |
| `docker-compose.prod.yml` | Prod hardening overlay: read-only rootfs, `no-new-privileges`, `cap_drop: ALL`, ulimits, mandatory `IMAGE_TAG` |
| `env.example` | Non-secret Compose variables → `/opt/nomaos/.env` |
| `env.otel.example` | The `OTEL_*` block → `/opt/nomaos/.env.otel` |
| `alloy/config.alloy` | Grafana Alloy: OTLP in (4317/4318), Tempo + Prometheus + Loki out |
| `render-secrets-from-vault.sh` | AppRole login → KV v2 read → `/opt/nomaos/.env.api` (mode 0600) |
| `deploy.sh` | ECR login → render secrets → pull → recreate → health gate → rollback on failure |
| `rollback.sh` | Redeploy `PREVIOUS_IMAGE_TAG` (or any tag) with the same health gate |
| `vault-policy.hcl` | The Vault policy + AppRole definition, and the full list of required secret keys |
| `_lib.sh` | Shared shell helpers (logging, secret scrubbing, health polling, `.env` editing) |

---

## 2. Host layout

```
/opt/nomaos/
├── docker-compose.yml          rsync'd from this directory by CI
├── docker-compose.prod.yml
├── deploy.sh  rollback.sh  render-secrets-from-vault.sh  _lib.sh
├── alloy/config.alloy
├── .env                        non-secret, 0644 — ECR_REGISTRY, IMAGE_TAG, ports
├── .env.api                    RENDERED FROM VAULT, 0600, never in git
├── .env.otel                   OTEL_* block, non-secret
├── vault-approle               0600 — role_id / secret_id (host-side credentials)
├── PREVIOUS_IMAGE_TAG          rollback pointer
├── CURRENT_IMAGE_TAG           what is running now
└── logs/                       owned by 1654:1654 when the prod overlay is used
```

### One-time provisioning

```bash
sudo install -d -m 0755 -o root -g root /opt/nomaos
sudo install -d -m 0755 -o 1654 -g 1654 /opt/nomaos/logs

# Compose files + scripts (CI rsyncs these on every deploy as well)
sudo rsync -a deploy/ec2/ /opt/nomaos/
sudo chmod +x /opt/nomaos/*.sh

# Non-secret configuration
sudo install -m 0644 /opt/nomaos/env.example      /opt/nomaos/.env
sudo install -m 0644 /opt/nomaos/env.otel.example /opt/nomaos/.env.otel
sudo $EDITOR /opt/nomaos/.env          # ECR_REGISTRY, ECR_REPOSITORY, VAULT_ADDR, …

# Vault AppRole credentials (host-side is the preferred model — CI never sees them)
sudo install -m 0600 /dev/null /opt/nomaos/vault-approle
printf 'VAULT_ROLE_ID=%s\nVAULT_SECRET_ID=%s\n' "$ROLE_ID" "$SECRET_ID" \
  | sudo tee /opt/nomaos/vault-approle >/dev/null
```

The instance profile needs `ecr:GetAuthorizationToken`,
`ecr:BatchGetImage`, `ecr:GetDownloadUrlForLayer` and
`ecr:BatchCheckLayerAvailability`, plus `ssm:UpdateInstanceInformation` &c. if
CI deploys over SSM.

---

## 3. Secrets — Vault only

Nothing sensitive lives in `appsettings*.json`, in the image, in Compose files
or in git. The application reads its entire configuration from
`kv/nomaos/<env>/appsettings` in two complementary ways:

1. **In-process** (`Program.cs`): when `ASPNETCORE_ENVIRONMENT != Development`
   the app adds `VaultSharp.Extensions.Configuration` with AppRole auth and a
   60-second reload interval, so a rotated secret reaches the running process
   without a restart.
2. **Rendered to `.env.api`** (`render-secrets-from-vault.sh`): flattens the KV
   document into `Section__Key=value` environment variables. This is what makes
   the container start correctly even when Vault is briefly unreachable at boot.

The in-process provider is registered **after** the environment-variable
provider, so Vault always wins — the rendered file is a warm cache, not a
competing source of truth.

```bash
# Render (deploy.sh calls this for you)
sudo /opt/nomaos/render-secrets-from-vault.sh \
  --addr https://vault.agrovesto.com \
  --mount kv \
  --path nomaos/prod/appsettings \
  --output /opt/nomaos/.env.api
```

The script logs in with AppRole, reads KV v2 (falling back to v1), writes the
file atomically with mode `0600`, and revokes its own token on exit. Multi-line
values (PEM keys) are deliberately skipped — `.env` files cannot represent them;
they stay in Vault for the in-process provider.

Required keys and the matching policy: see [`vault-policy.hcl`](./vault-policy.hcl).

---

## 4. Images in ECR

Every build pushes an immutable tag plus floating ones:

| Tag | Meaning |
|---|---|
| `sha-<short_sha>` | **Immutable.** What deploys and what rollback pins to |
| `branch-<slug>`, `build-<run_number>` | Traceability |
| `dev` / `prod` | Floating pointer to the newest image on that branch |
| `latest` | Convenience only — never deploy from it |

Manual push:

```bash
aws ecr get-login-password --region eu-west-1 \
  | docker login --username AWS --password-stdin "$ECR_REGISTRY"

docker buildx build \
  --platform linux/amd64 \
  -f Agrovesto.Backend.API/Dockerfile \
  -t "$ECR_REGISTRY/$ECR_REPOSITORY:sha-$(git rev-parse --short=7 HEAD)" \
  --build-arg COMMIT_SHA="$(git rev-parse HEAD)" \
  --push .
```

---

## 5. Deploying

```bash
cd /opt/nomaos
sudo ./deploy.sh --tag sha-1a2b3c4          # the only argument that matters
```

What it does, in order:

1. `aws ecr get-login-password | docker login` (instance profile credentials).
2. `render-secrets-from-vault.sh` → `.env.api` (skip with `--skip-vault`).
3. Writes `IMAGE_TAG` into `.env`, keeping the old value in `PREVIOUS_IMAGE_TAG`.
4. `docker compose pull api` — a failed pull restores the previous tag and stops
   before anything is recreated.
5. `docker compose up -d --no-deps --force-recreate api`.
6. Polls `http://127.0.0.1:8080/health/live` until healthy, then
   `/health/ready` (`HEALTH_TIMEOUT`, default 180s).
7. On failure: dumps `docker compose ps`, the last 200 log lines and the
   container's health log, rolls back to `PREVIOUS_IMAGE_TAG`, exits non-zero.
8. On success: prints `/status` (version, git SHA, uptime, per-check latency).

Useful flags: `--service <name>`, `--timeout <seconds>`, `--no-rollback`
(leave the broken container up for debugging), `--skip-vault`.

Exit codes: `0` deployed · `1` deploy failed and was rolled back (also usage
errors) · `2` deploy failed **and** the rollback failed — the service is down,
page someone.

### Prod overlay

`deploy.sh` includes `docker-compose.prod.yml` automatically when it exists.
Set `COMPOSE_OVERLAY=none` to run the base file only.

---

## 6. Rolling back

```bash
sudo ./rollback.sh                 # back to PREVIOUS_IMAGE_TAG
sudo ./rollback.sh --tag sha-9f8e7d6
sudo ./rollback.sh --list          # show current/previous pointers
```

`rollback.sh` health-gates exactly like a deploy and swaps the pointers, so a
second `rollback.sh` rolls *forward* again.

**Manual one-liner** (when the scripts themselves are the problem):

```bash
cd /opt/nomaos && \
  sed -i "s|^IMAGE_TAG=.*|IMAGE_TAG=$(cat PREVIOUS_IMAGE_TAG)|" .env && \
  docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --no-deps --force-recreate api && \
  curl -fsS http://127.0.0.1:8080/health/ready
```

---

## 7. Observability

The app speaks OTLP only; the collector is Grafana Alloy.

```
API ──OTLP gRPC :4317 / HTTP :4318──► Alloy ──► Tempo      (traces)
                                            ├─► Prometheus (metrics, remote_write)
                                            └─► Loki       (logs, from the json-file driver)
```

* Configuration is the standard `OTEL_*` block only — see
  [`env.otel.example`](./env.otel.example). No bespoke keys.
* Propagation is W3C TraceContext + Baggage.
* Instrumented: ASP.NET Core, HttpClient, `Microsoft.Data.SqlClient` (statement
  text sanitised), StackExchange.Redis, plus runtime and process metrics.
* `/health*`, `/status` and `/hangfire` are excluded from tracing — probe spans
  at 3 requests/second would otherwise be 90% of Tempo's ingest.
* Logs are one JSON object per line on stdout (Serilog `JsonFormatter`, with
  `trace_id`/`span_id`/`git.sha`/`deployment.environment` on every line), picked
  up by the `json-file` driver and shipped by Alloy's `loki.source.docker`.
* `GET /internal/telemetry/demo` emits a manual span plus the custom counter
  (`nomaos.onboarding.steps`) and histogram (`nomaos.onboarding.step.duration`) — the fastest way to prove the pipeline end to end after a
  change.

Run the sidecar collector with:

```bash
docker compose --profile alloy up -d
```

### Grafana runbook

| Question | Where |
|---|---|
| Is the deploy healthy? | `curl -s localhost:8080/status | jq` |
| Traces for one request | Tempo → search `service.name = nomaos-onboarding-api`, filter by `http.route` |
| Logs for one trace | Loki → `{app="nomaos-onboarding-api"} | json | trace_id="<id>"` |
| Error rate | Prometheus → `sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m]))` |
| Latency p95 | `histogram_quantile(0.95, sum by (le, http_route) (rate(http_server_request_duration_seconds_bucket[5m])))` |
| Which commit is live? | `nomaos_*` metric resource attributes, or `/status` → `build.gitSha` |

---

## 8. Health endpoints

| Endpoint | Checks | 200 | 503 | Consumer |
|---|---|---|---|---|
| `/health/live` | self only | Healthy/Degraded | Unhealthy | Docker `HEALTHCHECK`, Compose |
| `/health/ready` | SQL + Redis + Vault | Healthy/**Degraded** | Unhealthy | nginx / ALB / `deploy.sh` |
| `/health` | live + ready | Healthy/Degraded | Unhealthy | legacy consumers |
| `/status` | everything + build info | always | never | dashboards, on-call |

Liveness never touches a dependency: gating container restarts on a database
blip would restart the whole fleet at once and turn a 30-second incident into a
cold-start outage.

---

## 9. Troubleshooting

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml ps
docker compose logs --tail=200 api
docker inspect --format '{{json .State.Health}}' nomaos-api | jq
curl -s localhost:8080/status | jq '.checks[] | {name, status, durationMs, error}'
```

| Symptom | Likely cause |
|---|---|
| `pull access denied` | ECR login expired (12h) or the instance profile lacks `ecr:*`; `deploy.sh` re-logs in every run |
| Container restarts every 45s | `/health/live` is failing — check the logs; it never depends on SQL/Redis/Vault, so it is the app itself |
| `/health/ready` 503, `vault` Unhealthy | Vault sealed or `VAULT_ADDR` unreachable from the instance |
| `/health/ready` 503, `Database` Unhealthy | RDS security group / credentials rotated in Vault but the container was not recreated |
| `.env.api` empty | AppRole login failed — run `render-secrets-from-vault.sh` by hand, the errors are scrubbed but explicit |
| Logs are not in Loki | Compose `logging.driver` must stay `json-file`; Alloy discovers by the `com.nomaos.app` label |
