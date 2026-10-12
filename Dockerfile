# syntax=docker/dockerfile:1.7

########################################
# STAGE 1 — dependency restore
# Isolated so source changes don't bust
# the NuGet layer cache.
########################################
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS restore

WORKDIR /src

# Only project files — cache is invalidated only when a .csproj changes,
# not on every source edit. Restoring the API project alone (rather than the
# .sln) pulls in Core + Infrastructure via ProjectReference and skips the
# test project entirely, which the runtime image has no use for.
COPY Agrovesto.Backend.API/*.csproj                Agrovesto.Backend.API/
COPY Agrovesto.Backend.Core/*.csproj               Agrovesto.Backend.Core/
COPY Agrovesto.Backend.Infrastructure/*.csproj     Agrovesto.Backend.Infrastructure/

RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet restore Agrovesto.Backend.API/Agrovesto.Backend.API.csproj


########################################
# STAGE 2 — build + publish
# Separate from restore so the NuGet
# cache layer is never dirtied by source.
########################################
FROM restore AS publish

# Build metadata — passed by CI, surfaced at runtime for traceability.
ARG COMMIT_SHA=unknown
ARG BUILD_VERSION=0.0.0
ARG BUILT_AT=unknown

# Copy full source on top of the already-restored tree
COPY . .

RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet publish Agrovesto.Backend.API/Agrovesto.Backend.API.csproj \
      -c Release \
      -o /app/publish \
      --no-restore \
      /p:UseAppHost=false \
      /p:EnableSourceControlManagerQueries=false \
      /p:PublishTrimmed=false

# Stamp the artifact the same way the systemd pipeline used to.
RUN printf '%s\n' "$COMMIT_SHA"    > /app/publish/.commit-sha \
 && printf '%s\n' "$BUILD_VERSION" > /app/publish/.version \
 && printf '%s\n' "$BUILT_AT"      > /app/publish/.deployed-at


########################################
# STAGE 3 — runtime
########################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS runtime

ARG COMMIT_SHA=unknown
ARG BUILD_VERSION=0.0.0
ARG BUILT_AT=unknown

LABEL org.opencontainers.image.title="nomaos-api" \
      org.opencontainers.image.source="https://github.com/Agrovesto/Nomaos" \
      org.opencontainers.image.revision="$COMMIT_SHA" \
      org.opencontainers.image.version="$BUILD_VERSION" \
      org.opencontainers.image.created="$BUILT_AT"

# curl is required by HEALTHCHECK and by the blue/green deploy gate.
# ca-certificates keeps outbound TLS (SendGrid, Prembly, S3, Vault) working.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl ca-certificates \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Kestrel binds HTTP only — TLS is terminated at the ALB / nginx edge.
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false
# Tune the GC for container memory limits
ENV DOTNET_GCConserveMemory=5
ENV DOTNET_GCHeapHardLimitPercent=75
# ThreadPool — prevents cold-start latency on first burst
ENV DOTNET_ThreadPool_UnfairSemaphoreSpinLimit=6

# Note on app.UseHttpsRedirection(): no HTTPS port is configured here on
# purpose. With none available the middleware skips redirecting and only logs
# "Failed to determine the https port for redirect" once — the same thing that
# happens under systemd today. Do NOT set ASPNETCORE_HTTPS_PORT: that would
# make the middleware 307 every proxied request, including the healthcheck.

COPY --from=publish /app/publish .

# Serilog rolling file sink target. Compose bind-mounts the host directory over
# this path; creating it here with the right owner means the app can still log
# when run without that mount. Deliberately NOT a VOLUME — that would leave a
# dangling anonymous volume behind on every deploy.
RUN mkdir -p /app/logs && chown -R $APP_UID:$APP_UID /app/logs

# /health/ready only evaluates checks tagged "critical" (DB + app components).
# The full /health also pings SendGrid, Termii, Prembly and S3 — a third-party
# blip there must not make Docker kill an otherwise-serving container.
HEALTHCHECK --interval=30s --timeout=5s --start-period=45s --retries=3 \
    CMD curl -fsS --max-time 4 http://127.0.0.1:5000/health/ready || exit 1

EXPOSE 5005

# UID form (not "app") so Kubernetes/ECS runAsNonRoot checks can verify it.
USER $APP_UID

# Array form — no shell spawned, SIGTERM reaches dotnet directly for graceful drain.
ENTRYPOINT ["dotnet", "Agrovesto.Backend.API.dll"]
