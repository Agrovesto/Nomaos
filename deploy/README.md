# NomaOS — Docker on EC2

Migration of the `Agrovesto.Backend.API` (and a second .NET service) from
**artifact + systemd blue/green** to **containers on the same EC2 box**, laid
out so the later move to **ECS** is a translation rather than a rewrite.

---

## What changed

| | Before | After |
|---|---|---|
| Artifact | `dotnet publish` rsync'd to `/var/www/Nomaos/releases/<sha>` | Immutable image in ECR |
| Slots | `Agrovesto.Backend.API-{blue,green}` systemd units on :5000/:5002 | `nomaos-api-{blue,green}` containers, both on :5000 internally |
| Traffic flip | host nginx `upstream` file + `nginx -s reload` | nginx **container**, same upstream-file + reload mechanic |
| Slot record | `/var/www/Nomaos/active-slot` | `/opt/nomaos/state/nomaos-api.active-slot` |
| SQL Server | installed on the host | `nomaos-mssql` container, named volumes |
| Redis | installed on the host | `nomaos-redis` container, AOF volume |
| Secrets | Vault AppRole via systemd `EnvironmentFile` | Vault AppRole via container env — **app code unchanged** |
| CI → host | SSH + rsync | ECR push + SSM `send-command` |

The blue/green *contract* is deliberately identical: bring the idle slot up,
prove it healthy, flip one upstream file, reload, retire the old slot.

---

## Layout

```
deploy/
├── compose/
│   ├── docker-compose.infra.yml    nginx + mssql + redis   (project: nomaos-infra)
│   ├── docker-compose.app.yml      one app slot            (project: <app>-<slot>)
│   └── docker-compose.migrate.yml  EF Core migration runner
├── nginx/templates/
│   └── default.conf.template       server blocks (envsubst at container start)
├── env/
│   ├── infra.env.example
│   └── app.env.example
├── scripts/
│   ├── bootstrap.sh                install Docker, create /opt/nomaos      (once)
│   ├── migrate-data.sh             host SQL/Redis -> containers            (once)
│   ├── deploy.sh                   blue/green deploy                       (every release)
│   ├── rollback.sh                 flip back
│   ├── status.sh                   what is running
│   ├── cutover-from-systemd.sh     retire the old stack                    (once, last)
│   └── lib.sh                      shared helpers
└── Makefile
```

Host state lives in `/opt/nomaos` (`NOMAOS_STATE_DIR`), never in the checkout:

```
/opt/nomaos/
├── env/       nomaos-api.env, infra.env      root:root 0600  ← secrets
├── nginx/upstreams/   nomaos_api.conf        ← rewritten on every flip
├── state/     nomaos-api.active-slot
├── backups/
└── logs/<app>/<slot>/
```

---

## Networks

Two bridges, so the database is not reachable from the edge:

- `nomaos_edge` — nginx ↔ app containers
- `nomaos_data` — app containers ↔ mssql / redis

App containers **publish no host ports**. nginx reaches them by container name.
That is precisely the ECS service/target-group model.

---

## Migration runbook

Each phase is independently reversible. Do not compress them into one window.

### Phase 0 — before you touch the server

Confirm the Vault AppRole and the KV path the app expects:

```bash
# on the EC2, as it runs today
sudo systemctl show Agrovesto.Backend.API-blue -p EnvironmentFiles
sudo grep -E 'VAULT_(ADDR|ROLE_ID|SECRET_ID)' /etc/default/Agrovesto.Backend.API
```

`Program.cs` reads `VAULT_ADDR` / `VAULT_ROLE_ID` / `VAULT_SECRET_ID` and mounts
`kv` at `nomaos/prod/appsettings`. Those three values move verbatim into
`/opt/nomaos/env/nomaos-api.env`. **No application code changes.**

> **Know this before you start.** There is no `appsettings.json` and no
> `appsettings.Production.json` in the repo — only `Development`, `Staging` and
> `dev`. With `ASPNETCORE_ENVIRONMENT=Production` the app therefore gets
> **100% of its configuration from Vault plus environment variables**: `Jwt:Key`,
> `CorsOrigins`, `AWS:*`, `SendGrid`, `TermiSMS`, `AesOptions`, all of it.
>
> That is unchanged by containerising — it is how the box already runs — but it
> means a Vault outage or a wrong AppRole is a hard startup failure, not a
> degradation. Confirm the KV payload is complete *before* Phase 5:
> ```bash
> vault kv get -mount=kv nomaos/prod/appsettings
> ```
>
> Two related quirks worth knowing:
> - `appsettings.Staging.json` contains `"Vault": { "Enabled": false }`, but
>   `Program.cs` never reads that flag — it keys off `!IsDevelopment()`. Staging
>   *will* attempt Vault.
> - Vault is added **after** the env-var provider, so any key Vault serves wins
>   over the same key in `docker-compose.app.yml`. See the troubleshooting note
>   on `ConnectionStrings__DefaultConnection`.

### Phase 1 — bootstrap (non-disruptive)

Installs Docker and creates `/opt/nomaos`. The systemd stack keeps serving.

```bash
sudo mkdir -p /opt/nomaos/repo
sudo git clone https://github.com/Agrovesto/Nomaos.git /opt/nomaos/repo
cd /opt/nomaos/repo
sudo ./deploy/scripts/bootstrap.sh
```

Then fill in the two env files it created:

```bash
sudo vi /opt/nomaos/env/infra.env        # MSSQL_SA_PASSWORD, server names
sudo vi /opt/nomaos/env/nomaos-api.env   # VAULT_*, DB_CONNECTION_STRING
```

> Quote `DB_CONNECTION_STRING` — it contains spaces and semicolons.

### Phase 2 — start data services (non-disruptive)

nginx will try to bind :80/:443, which the host nginx still owns. Start only
the data services for now:

```bash
cd /opt/nomaos/repo/deploy/compose
sudo docker compose -p nomaos-infra --env-file /opt/nomaos/env/infra.env \
  -f docker-compose.infra.yml up -d mssql redis
```

### Phase 3 — move the data

Backs up the host DB and restores it into the container. **The host instance is
never modified**, so rollback is a connection-string change.

```bash
cd /opt/nomaos/repo
sudo HOST_SA_PASSWORD='<host sa password>' \
  ./deploy/scripts/migrate-data.sh --db nomaosdb
```

It verifies the backup (`RESTORE VERIFYONLY`), compares row counts across both
instances, and reports orphaned users. Recreate any non-`sa` login it flags.

Redis is treated as disposable cache — the script tells you how to copy the RDB
if you decide you want the keys.

### Phase 4 — first container deploy (still not live)

Build and push an image, then deploy. nginx-container is not on :80 yet, so
this only proves the container runs.

```bash
sudo ./deploy/scripts/deploy.sh \
  --app nomaos-api \
  --image <acct>.dkr.ecr.eu-west-1.amazonaws.com/nomaos:<sha> \
  --commit <sha>
```

Verify from inside the box before exposing anything:

```bash
docker exec nomaos-api-blue curl -fsS localhost:5000/health/ready
sudo ./deploy/scripts/status.sh
docker logs --tail 100 nomaos-api-blue     # confirm Vault loaded, DB connected
```

### Phase 5 — cutover (the live moment)

Hands :80/:443 from host nginx to the nginx container.

```bash
sudo ./deploy/scripts/cutover-from-systemd.sh            # dry run first
sudo ./deploy/scripts/cutover-from-systemd.sh --commit
```

Reverse within seconds if anything is wrong:

```bash
sudo ./deploy/scripts/cutover-from-systemd.sh --undo
```

### Phase 6 — decommission (days later, once confident)

```bash
sudo systemctl disable --now mssql-server
sudo systemctl disable --now redis-server
sudo rm -rf /var/www/Nomaos/releases/*
```

---

## Day-to-day

```bash
cd /opt/nomaos/repo/deploy

make status                                     # what is live
make deploy APP=nomaos-api IMAGE=<ref>:<tag>    # blue/green release
make rollback APP=nomaos-api                    # flip back
make logs APP=nomaos-api                        # tail the active slot
make migrate                                    # apply EF migrations
make backup-db                                  # ad-hoc DB backup
make shell-db / make shell-redis
```

---

## Database migrations

`Program.cs` never calls `Database.Migrate()`, and that stays true. During a
flip both builds are briefly live, so **the schema must be compatible with the
old and new code at once**. Apply migrations as a deliberate step *before*
deploying code that depends on them:

```bash
make migrate
```

Expand/contract is the rule: add columns nullable, backfill, deploy code, drop
in a later release.

---

## Adding the second app

1. `sudo cp /opt/nomaos/env/nomaos-api.env /opt/nomaos/env/nomaos-app2.env`
2. Edit: `APP_NAME=nomaos-app2`, `UPSTREAM_NAME=nomaos_app2`, its own Vault path/creds
3. Set `NOMAOS_APP2_SERVER_NAME` in `infra.env` to its real hostname, recreate nginx
4. `sudo ./deploy/scripts/deploy.sh --app nomaos-app2 --image <ref>:<tag>`

The compose file, nginx template and every script already handle it — nothing
to duplicate. Both apps can use port 5000 internally because each container has
its own network namespace.

---

## Troubleshooting

**Container healthy but nginx 502s**
The upstream file names a container that is not on `nomaos_edge`:
```bash
docker inspect -f '{{json .NetworkSettings.Networks}}' nomaos-api-blue | jq keys
cat /opt/nomaos/nginx/upstreams/nomaos_api.conf
```

**`nginx -s reload` fails after a flip**
nginx resolves upstream hostnames at config-load time. `deploy.sh` only ever
writes the *serving* slot into the upstream file for this reason. If you hand-
edit it to reference a stopped container, the reload fails.

**App cannot reach the database**
Inside a container `localhost` is the container. Use the service names
`mssql` / `redis`. Check the value that actually arrived:
```bash
docker exec nomaos-api-blue printenv ConnectionStrings__DefaultConnection
```
Remember Vault wins over env vars — `AddVaultConfiguration` is registered after
the env-var provider, so if Vault serves
`ConnectionStrings:DefaultConnection` it overrides the compose value. Update
the Vault KV entry too, or the container will keep dialling `localhost,1433`.

**SQL Server container will not start**
Almost always the SA password policy (8+ chars, 3 of upper/lower/digit/symbol)
or volume permissions:
```bash
docker logs nomaos-mssql | tail -40
```

**Disk filling up**
`bootstrap.sh` configures json-file rotation and logrotate, but images
accumulate:
```bash
docker system df
docker image prune -a --filter "until=168h"
```

---

## Path to ECS

This layout was chosen so the migration is mostly mechanical:

| Here | ECS |
|---|---|
| `docker-compose.app.yml` `environment:` | task definition `environment` |
| Vault `VAULT_*` env | keep, or swap to Secrets Manager `secrets` |
| nginx upstream flip | ALB target group + CodeDeploy blue/green |
| `deploy.sh` health gate | target group health check |
| `nomaos-mssql` container | **RDS** — do not run SQL Server as an ECS task |
| `nomaos-redis` container | **ElastiCache** |
| `/app/logs` bind mount | `awslogs` driver → CloudWatch |

Two things to do before that move, both already set up here:

- **Stop writing logs to a file.** The container already logs to stdout;
  `LOG_FILE_PATH` is optional and should be dropped on ECS.
- **Externalise the data.** The compose volumes are the only stateful thing on
  the box. Point `DB_CONNECTION_STRING` at RDS and
  `REDIS_CONNECTION_STRING` at ElastiCache, and the app containers become
  fully stateless — which is the actual precondition for ECS.

---

## CI workflow

The pipeline is installed at **`.github/workflows/docker-deploy.yml`**:

- pull requests targeting `devops` build the Docker image only — no AWS
  credentials, no deployment;
- a push to `devops` builds and blue/green deploys to the `staging` GitHub
  environment;
- a push to `master` (or a manual `workflow_dispatch` with `environment: prod`)
  deploys to the `prod` environment.

Configure the `staging` and `prod` environments with:

Required repository/environment settings:

| Kind | Name | Value |
|---|---|---|
| Secret | `AWS_DEPLOY_ROLE_ARN` | IAM role trusted for GitHub OIDC |
| Secret | `EC2_INSTANCE_ID` | `i-…` of the target box |
| Variable | `HEALTH_URL` | e.g. `https://noma-api.nomaos.com/health` |

The IAM role needs `ecr:*` on the `agrovesto-dev-nomaos` repository plus
`ssm:SendCommand` / `ssm:ListCommandInvocations` on the instance. The EC2
instance profile needs `AmazonSSMManagedInstanceCore` and ECR pull rights.

The old rsync + systemd production pipeline (`prod.yml`) has been removed — the
Docker workflow is the only pipeline that deploys production. The `dev` branch
still deploys through the rsync pipeline in `.github/workflows/dev.yml`; keep
exactly one pipeline active per branch so two never deploy the same box.
