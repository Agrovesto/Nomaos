# syntax=docker/dockerfile:1.7

########################################
# STAGE 1 — dependency restore
# SDK selection is controlled by global.json
# and copied before restore so restore and
# publish resolve the same SDK.
########################################
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS restore

WORKDIR /src

# IMPORTANT:
# global.json must exist before dotnet restore so the
# restore stage uses the same SDK selection as publish.
COPY global.json ./

# Project files only — keeps NuGet restore cacheable
# across normal source changes.
COPY Agrovesto.Backend.API/*.csproj            Agrovesto.Backend.API/
COPY Agrovesto.Backend.Core/*.csproj           Agrovesto.Backend.Core/
COPY Agrovesto.Backend.Infrastructure/*.csproj Agrovesto.Backend.Infrastructure/

# Show the selected SDK explicitly in CI logs.
RUN set -eux; \
    echo "=== .NET SDK ==="; \
    dotnet --version; \
    echo "=== .NET SDK INFO ==="; \
    dotnet --info; \
    echo "=== RESTORE ==="; \
    dotnet restore Agrovesto.Backend.API/Agrovesto.Backend.API.csproj


########################################
# STAGE 2 — build + publish
########################################
FROM restore AS publish

# Copy full application source on top of the restored project tree.
# Non-application files (tests, deployment scripts, docs) are excluded by
# .dockerignore so changes to them do not invalidate this expensive layer.
COPY . .

# Verify that the repository global.json is present
# and that the exact same SDK resolution is being used.
RUN set -eux; \
    echo "=== global.json ==="; \
    cat global.json; \
    echo "=== SELECTED SDK ==="; \
    dotnet --version; \
    echo "=== INSTALLED SDKS ==="; \
    dotnet --list-sdks; \
    echo "=== PUBLISH ==="; \
    dotnet publish Agrovesto.Backend.API/Agrovesto.Backend.API.csproj \
      -c Release \
      -o /app/publish \
      --no-restore \
      -maxcpucount \
      /p:UseAppHost=false \
      /p:EnableSourceControlManagerQueries=false \
      /p:PublishTrimmed=false

# Keep build metadata out of /app/publish: changing a commit SHA or timestamp
# would otherwise change the large application filesystem layer and force it to
# be pushed again. Metadata is stored in the image config below instead.

########################################
# STAGE 3 — runtime
########################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS runtime

ARG COMMIT_SHA=unknown
ARG BUILD_VERSION=0.0.0
ARG BUILT_AT=unknown

# BuildInfo reads this environment variable before falling back to a file.
ENV COMMIT_SHA=$COMMIT_SHA

LABEL org.opencontainers.image.title="nomaos-api" \
      org.opencontainers.image.source="https://github.com/Agrovesto/Nomaos" \
      org.opencontainers.image.revision="$COMMIT_SHA" \
      org.opencontainers.image.version="$BUILD_VERSION" \
      org.opencontainers.image.created="$BUILT_AT"

# curl is required by HEALTHCHECK and by the blue/green deploy gate.
# ca-certificates keeps outbound TLS working.
RUN apt-get update \
 && apt-get install -y --no-install-recommends \
      curl \
      ca-certificates \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Kestrel listens on HTTP only.
# TLS is terminated by the external edge.
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false

# GC / container tuning.
ENV DOTNET_GCConserveMemory=5
ENV DOTNET_GCHeapHardLimitPercent=75

# ThreadPool tuning.
ENV DOTNET_ThreadPool_UnfairSemaphoreSpinLimit=6

# HTTPS is intentionally not configured inside the container.
# The reverse proxy terminates TLS.
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

COPY --from=publish /app/publish .

# Serilog rolling file sink target.
# The compose deployment bind-mounts over this directory.
RUN mkdir -p /app/logs \
 && chown -R $APP_UID:$APP_UID /app/logs

# Container health check.
#
# /health/ready contains only critical checks:
# database + application components.
#
# Full /health includes external dependencies and is intentionally
# not used here so a third-party outage doesn't cause Docker to
# replace an otherwise healthy application container.
HEALTHCHECK \
    --interval=30s \
    --timeout=5s \
    --start-period=45s \
    --retries=3 \
    CMD curl -fsS --max-time 4 \
        http://127.0.0.1:5000/health/ready || exit 1

EXPOSE 5000

# Use the image-provided non-root UID.
# This is also compatible with ECS/Kubernetes runAsNonRoot checks.
USER $APP_UID

# Direct exec form so SIGTERM reaches dotnet directly.
ENTRYPOINT ["dotnet", "Agrovesto.Backend.API.dll"]