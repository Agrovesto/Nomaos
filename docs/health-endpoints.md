# Health endpoints

Four endpoints, four different consumers, one rule each. They are built on
`Microsoft.Extensions.Diagnostics.HealthChecks` and wired from `Program.cs`
through `AddNomaosProbeChecks()` and `MapNomaosHealthEndpoints()` in
`Agrovesto.Backend.API/Health/`.

| Endpoint | Checks run | 200 | 503 | Consumer |
|---|---|---|---|---|
| `GET /health/live` | tag `live` — the self-check only | Healthy, Degraded | Unhealthy | Docker `HEALTHCHECK`, Compose, `deploy.sh` |
| `GET /health/ready` | tag `ready` — SQL, Redis, Vault | Healthy, **Degraded** | Unhealthy | nginx / ALB, `deploy.sh` |
| `GET /health` | `live` ∪ `ready` | Healthy, Degraded | Unhealthy | legacy consumers |
| `GET /status` | everything, plus build info | **always** | never | dashboards, on-call |

All four are anonymous, never cached (`Cache-Control: no-store`) and return
`application/json; charset=utf-8`.

## The two rules that matter

**Liveness never touches a dependency.** `/health/live` runs
`SelfLivenessHealthCheck` and nothing else: no SQL, no Redis, no Vault, no HTTP.
Docker restarts the container when liveness fails, so a check that can fail for
an external reason would restart every container in the fleet the moment RDS
fails over — turning a 30-second blip into a cold-start outage.

**Degraded still serves traffic.** Only `Unhealthy` returns 503 and pulls the
instance out of rotation. A slow-but-answering Vault, an unconfigured Redis in
Development, or a dead third-party API (SendGrid, Termii — all tagged `status`,
never `ready`) leave the instance in the pool.

## Tags

| Tag | Meaning | Examples |
|---|---|---|
| `live` | process self-check | `self` |
| `ready` | must be up before this instance takes traffic | `Database`, `SQL Server Database`, `redis`, `vault` |
| `status` | informational, non-gating | `SendGrid API`, `Termii SMS API`, `AWS S3`, `IdentityPass API` |

## Bodies

```jsonc
// GET /health/live
{ "status": "Healthy", "timestamp": "2026-09-25T12:30:45.123Z", "totalDurationMs": 0.4 }
```

```jsonc
// GET /health/ready
{
  "status": "Degraded",
  "ready": true,
  "timestamp": "2026-09-25T12:30:45.123Z",
  "totalDurationMs": 12.9,
  "checks": [
    { "name": "redis",  "status": "Healthy",  "durationMs": 2.5, "description": "Redis responded.",
      "tags": ["ready"], "data": { "latencyMs": "2.5", "endpoint": "nomaos-cache…:6379" } },
    { "name": "vault",  "status": "Degraded", "durationMs": 1.1,
      "description": "VAULT_ADDR is not configured; Vault-backed configuration is disabled.", "tags": ["ready"] }
  ]
}
```

```jsonc
// GET /status  (always 200 — it reports, it does not gate)
{
  "status": "Degraded",
  "live": true,
  "ready": true,
  "timestamp": "2026-09-25T12:30:45.123Z",
  "totalDurationMs": 42.5,
  "build": {
    "application": "nomaos-onboarding-api", "version": "1.4.2", "gitSha": "abc1234",
    "environment": "Production", "machineName": "ip-10-0-1-42",
    "startedAtUtc": "2026-09-25T10:00:00.000Z", "uptimeSeconds": 9045.123, "uptime": "0d 02h 30m 45s"
  },
  "memory": { "workingSetMb": 150, "gcHeapMb": 50, "threadCount": 24 },
  "summary": { "total": 11, "healthy": 9, "degraded": 2, "unhealthy": 0 },
  "checks": [ /* every check, with per-check latency in ms */ ]
}
```

Secrets never appear in a body: the Redis endpoint is truncated at the first
comma (dropping `password=…`), and exception **messages** are surfaced while
stack traces are not.

## Individual checks

| Check | What it does | Degraded when |
|---|---|---|
| `self` | Returns uptime, version, git SHA. Always Healthy | never |
| `Database` / `SQL Server Database` | EF `CanConnect` + `SELECT 1` | replaced by a Degraded stub when no `DefaultConnection` is configured (CI smoke test) |
| `redis` | `IDistributedCache.GetAsync` on a key that never exists — the same connection pool the app uses, no extra sockets | no cache registered, or no `Redis:ConnectionString` |
| `vault` | Unauthenticated `GET /v1/sys/health?standbyok=true&perfstandbyok=true` | `VAULT_ADDR` unset; unexpected status code |

Vault status mapping: `200`/`429`/`472`/`473` → Healthy (active, standby, DR
secondary, performance standby), `503` → Unhealthy (sealed), `501` → Unhealthy
(uninitialised), anything else → Degraded. A probe cancelled by shutdown is
Degraded, never Unhealthy, so a drain does not flap readiness.

## Legacy endpoints

`HealthController` keeps `/health/detailed`, `/health/status`, `/health/database`,
`/health/external`, `/health/endpoints`, `/health/components`, `/health/critical`
and `/health/summary` (the old `/health` body, moved aside so the probe
endpoint above can own `/health`).

## Tests

`NomaOS.Test/Health/` covers every branch, including the full deployment
matrix over a real `TestServer` pipeline (`HealthEndpointsIntegrationTests`):
dependencies healthy → all 200; dependencies down → live 200, ready 503, status
200/Unhealthy; unconfigured → ready 200/Degraded. No real SQL, Redis, Vault or
socket is involved.
