# syntax=docker/dockerfile:1.7

ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:8.0.423-alpine
ARG DOTNET_RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:8.0-alpine

########################################
# STAGE 1 — RESTORE
########################################

FROM --platform=$BUILDPLATFORM ${DOTNET_SDK_IMAGE} AS restore

ARG TARGETARCH

WORKDIR /src

# SDK selection must be identical during restore and publish.
COPY global.json ./

# Copy project files first so source-code changes do not invalidate
# the expensive NuGet restore layer.
COPY Agrovesto.Backend.API/*.csproj Agrovesto.Backend.API/
COPY Agrovesto.Backend.Core/*.csproj Agrovesto.Backend.Core/
COPY Agrovesto.Backend.Infrastructure/*.csproj Agrovesto.Backend.Infrastructure/

# If these files exist in the repository and affect dependency resolution,
# uncomment them:
#
# COPY NuGet.config ./
# COPY Directory.Build.props ./
# COPY Directory.Build.targets ./
# COPY Directory.Packages.props ./

RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    echo "=== .NET SDK ===" \
 && dotnet --version \
 && echo "=== INSTALLED SDKS ===" \
 && dotnet --list-sdks \
 && echo "=== RESTORE ===" \
 && dotnet restore \
      Agrovesto.Backend.API/Agrovesto.Backend.API.csproj \
      -a "$(case "$TARGETARCH" in \
            amd64) echo x64 ;; \
            arm64) echo arm64 ;; \
            *) echo x64 ;; \
          esac)"


########################################
# STAGE 2 — PUBLISH
########################################

FROM restore AS publish

ARG TARGETARCH
ARG COMMIT_SHA=unknown
ARG BUILD_VERSION=0.0.0
ARG BUILT_AT=unknown

WORKDIR /src

# Copy the complete source only after restore.
COPY . .

RUN echo "=== global.json ===" \
 && cat global.json \
 && echo "=== SELECTED SDK ===" \
 && dotnet --version \
 && echo "=== INSTALLED SDKS ===" \
 && dotnet --list-sdks \
 && echo "=== PUBLISH ===" \
 && dotnet publish \
      Agrovesto.Backend.API/Agrovesto.Backend.API.csproj \
      -c Release \
      -a "$(case "$TARGETARCH" in \
            amd64) echo x64 ;; \
            arm64) echo arm64 ;; \
            *) echo x64 ;; \
          esac)" \
      -o /app/publish \
      --no-restore \
      --no-self-contained \
      /p:UseAppHost=false \
      /p:EnableSourceControlManagerQueries=false \
      /p:PublishTrimmed=false \
      /p:DebugType=None \
      /p:DebugSymbols=false \
      /p:InformationalVersion="${BUILD_VERSION}+${COMMIT_SHA}"

# Build metadata.
RUN printf '%s\n' "$COMMIT_SHA" \
      > /app/publish/.commit-sha \
 && printf '%s\n' "$BUILD_VERSION" \
      > /app/publish/.version \
 && printf '%s\n' "$BUILT_AT" \
      > /app/publish/.deployed-at


########################################
# STAGE 3 — RUNTIME
########################################

FROM ${DOTNET_RUNTIME_IMAGE} AS runtime

ARG COMMIT_SHA=unknown
ARG BUILD_VERSION=0.0.0
ARG BUILT_AT=unknown

LABEL org.opencontainers.image.title="nomaos-onboarding-api" \
      org.opencontainers.image.description="NomaOS Onboarding API (ASP.NET Core 8)" \
      org.opencontainers.image.source="https://github.com/Agrovesto/Nomaos" \
      org.opencontainers.image.revision="$COMMIT_SHA" \
      org.opencontainers.image.version="$BUILD_VERSION" \
      org.opencontainers.image.created="$BUILT_AT" \
      org.opencontainers.image.vendor="Agrovesto"

########################################
# Runtime dependencies
########################################

RUN apk add --no-cache \
      ca-certificates \
      curl \
      icu-data-full \
      icu-libs \
      tzdata \
      libstdc++ \
      libgcc \
      fontconfig \
      ttf-dejavu \
 && update-ca-certificates

########################################
# Non-root application user
########################################

RUN addgroup -g 1654 -S app \
 && adduser \
      -u 1654 \
      -S \
      -G app \
      -h /app \
      -s /sbin/nologin \
      app

WORKDIR /app

########################################
# Application environment
########################################

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_PRINT_TELEMETRY_MESSAGE=false \
    DOTNET_GCConserveMemory=5 \
    DOTNET_GCHeapHardLimitPercent=75 \
    TZ=UTC

########################################
# Application
########################################

COPY --from=publish \
     --chown=1654:1654 \
     /app/publish/ \
     /app/

########################################
# Application logs
########################################

RUN mkdir -p /app/logs \
 && chown -R 1654:1654 /app/logs

########################################
# Network
########################################

EXPOSE 8080

########################################
# Container health
########################################

# Liveness only.
# Do not use /health/ready here because dependency failures such as
# SQL/Redis/Vault outages should not cause Docker to restart the container.

HEALTHCHECK \
    --interval=30s \
    --timeout=5s \
    --start-period=45s \
    --retries=3 \
    CMD curl -fsS \
        --max-time 4 \
        http://127.0.0.1:8080/health/live \
        || exit 1

########################################
# Graceful shutdown
########################################

STOPSIGNAL SIGTERM

########################################
# Security
########################################

USER 1654:1654

########################################
# Application entrypoint
########################################

ENTRYPOINT ["dotnet", "Agrovesto.Backend.API.dll"]

