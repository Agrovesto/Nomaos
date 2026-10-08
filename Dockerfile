# syntax=docker/dockerfile:1.7

########################################
# STAGE 1 — BUILD
########################################
FROM mcr.microsoft.com/dotnet/sdk:8.0.425-jammy AS build

WORKDIR /src

########################################
# COPY ENTIRE SOURCE TREE
#
# This guarantees restore and publish see
# exactly the same project configuration.
########################################
COPY . .

########################################
# VERIFY SDK
########################################
RUN set -eux; \
    echo "=== global.json ==="; \
    cat global.json; \
    echo "=== SELECTED SDK ==="; \
    dotnet --version; \
    echo "=== INSTALLED SDKS ==="; \
    dotnet --list-sdks

########################################
# RESTORE
#
# NuGet cache survives across builds.
########################################
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    set -eux; \
    echo "=== RESTORE ==="; \
    dotnet restore \
      Agrovesto.Backend.API/Agrovesto.Backend.API.csproj

########################################
# PUBLISH
########################################
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    set -eux; \
    echo "=== PUBLISH ==="; \
    dotnet publish \
      Agrovesto.Backend.API/Agrovesto.Backend.API.csproj \
      -c Release \
      -o /app/publish \
      --no-restore \
      -maxcpucount \
      /p:UseAppHost=false \
      /p:EnableSourceControlManagerQueries=false \
      /p:PublishTrimmed=false \
      /p:DebugType=None \
      /p:DebugSymbols=false

########################################
# STAGE 2 — RUNTIME
########################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0.31-jammy AS runtime

ARG COMMIT_SHA=unknown
ARG BUILD_VERSION=0.0.0
ARG BUILT_AT=unknown

ENV COMMIT_SHA=$COMMIT_SHA

LABEL org.opencontainers.image.title="nomaos-api" \
      org.opencontainers.image.source="https://github.com/Agrovesto/Nomaos" \
      org.opencontainers.image.revision="$COMMIT_SHA" \
      org.opencontainers.image.version="$BUILD_VERSION" \
      org.opencontainers.image.created="$BUILT_AT"

########################################
# RUNTIME DEPENDENCIES
########################################
RUN apt-get update \
 && apt-get install -y --no-install-recommends \
      curl \
      ca-certificates \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app

########################################

    # APPLICATION RUNTIME
    # Environment-specific values such as:
    # ASPNETCORE_ENVIRONMENT
    # VAULT_ADDR
    # VAULT_ROLE_ID
    # VAULT_SECRET_ID
    # VAULT_SECRET_PATH
    # are injected at deployment/runtime.
########################################

########################################
# APPLICATION CONFIGURATION
########################################
ENV ASPNETCORE_URLS=http://+:5000 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_PRINT_TELEMETRY_MESSAGE=false \
    DOTNET_GCConserveMemory=5 \
    DOTNET_GCHeapHardLimitPercent=75 \
    DOTNET_ThreadPool_UnfairSemaphoreSpinLimit=6 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

########################################
# COPY PUBLISHED APPLICATION
########################################
COPY --from=build /app/publish/ .

########################################
# LOG DIRECTORY
########################################
RUN mkdir -p /app/logs \
 && chown -R $APP_UID:$APP_UID /app/logs

########################################
# HEALTHCHECK
########################################
HEALTHCHECK \
    --interval=10s \
    --timeout=5s \
    --start-period=15s \
    --retries=3 \
    CMD curl -fsS --max-time 4 \
        http://127.0.0.1:5000/health/ready || exit 1

EXPOSE 5000

########################################
# NON-ROOT
########################################
USER $APP_UID

########################################
# START APPLICATION
########################################
ENTRYPOINT ["dotnet", "Agrovesto.Backend.API.dll"]