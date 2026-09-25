# Container image

`Agrovesto.Backend.API/Dockerfile` is the image that ships. (The Dockerfile at
the repository root is the older jammy/port-5000 build used by the previous
tooling and is left untouched.)

**Build context is the repository root**, because the API project references
`Agrovesto.Backend.Core` and `Agrovesto.Backend.Infrastructure`:

```bash
docker build -f Agrovesto.Backend.API/Dockerfile -t nomaos-api:dev .
```

## Stages

| Stage | Base | Job |
|---|---|---|
| `restore` | `sdk:8.0-alpine` **on `$BUILDPLATFORM`** | copies the three `.csproj` files only, then `dotnet restore -a <arch>` |
| `publish` | same | copies the sources, `dotnet publish -c Release -a <arch> --no-restore /p:UseAppHost=false`, stamps `.commit-sha` / `.version` / `.deployed-at` |
| `runtime` | `aspnet:8.0-alpine` | the shipped image: publish output, `curl`, ICU, tzdata, CA certificates, non-root user |

### Why `--platform=$BUILDPLATFORM` and `dotnet publish -a`

The SDK stage always runs on the **builder's** architecture and cross-compiles
to the target with `-a x64|arm64`. `docker buildx build --platform
linux/amd64,linux/arm64` therefore needs no QEMU: an arm64 image is produced by
a native-speed x64 compiler. QEMU-emulated `dotnet publish` is roughly an order
of magnitude slower and occasionally hangs on the Roslyn server — it is not used
here at all.

EC2 is amd64, so CI only pushes `linux/amd64`; the arm64 path exists for local
Apple-silicon builds and for a future Graviton move.

### Runtime hardening

* `USER 1654:1654` — a fixed non-root uid/gid, so bind-mounted `logs/` can be
  pre-chowned on the host.
* No SDK, no git, no source, no secrets in the final layer.
* `ASPNETCORE_URLS=http://+:8080`, `EXPOSE 8080` — an unprivileged port, which
  is what lets the container drop to a non-root user at all.
* `STOPSIGNAL SIGTERM` + exec-form `ENTRYPOINT` → the signal reaches `dotnet` as
  PID 1 and Kestrel drains within `stop_grace_period`.
* `HEALTHCHECK curl -fsS http://127.0.0.1:8080/health/live` — liveness only; see
  [health-endpoints.md](./health-endpoints.md) for why readiness must not gate
  container restarts.
* `icu-data-full` + `icu-libs` + `tzdata`: the app formats Nigerian dates and
  currency and runs with `TZ=Africa/Lagos`; without ICU, Alpine silently falls
  back to invariant globalization and money formats change.

### `global.json`

The repository pins the SDK to `8.0.423` with `"rollForward": "disable"` so every
developer machine uses the same compiler. Inside the image the *base tag* is the
pin, and the SDK it ships is whichever 8.0.x patch Microsoft published last — so
the publish stage rewrites `rollForward` to `latestMajor` **for the container
build only**. The file in git is untouched, and `TargetFramework=net8.0` still
fixes the output. Without this, a base-image refresh breaks the build with
"compatible SDK version not found" for a reason unrelated to the code.

### Build arguments

| Arg | Purpose |
|---|---|
| `COMMIT_SHA` | stamped into `/app/.commit-sha`, surfaced by `/status` and on every log line |
| `BUILD_VERSION` | informational assembly version |
| `BUILT_AT` | build timestamp label |
| `DOTNET_SDK_IMAGE` / `DOTNET_RUNTIME_IMAGE` | mirror or base-image overrides. Both are Alpine (musl); override **both** together or the RID will not match |

### Layer caching

`.csproj` files are copied and restored before any source, so a code-only change
reuses the restore layer. BuildKit cache mounts (`--mount=type=cache,target=/root/.nuget/packages`)
keep the NuGet cache across builds; in CI the same effect is achieved with
`cache-from/to: type=gha`.

## Running it locally

```bash
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e OTEL_SDK_DISABLED=true \
  -e AWS_LOGS_ENABLED=false \
  nomaos-api:dev

curl -s localhost:8080/health/live | jq
```

With no `ConnectionStrings:DefaultConnection`, the app deliberately skips
Hangfire, the database-backed hosted services and the SQL health check, and
still answers `/health/live` — this is exactly what the CI smoke test asserts.
Give it a connection string and all of them come back.

For the Compose deployment on EC2 see [`deploy/ec2/README.md`](../deploy/ec2/README.md).
