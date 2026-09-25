# syntax=docker/dockerfile:1.7
# ─────────────────────────────────────────────────────────────────────────────
# NomaOS Onboarding API — production image
#
#   Build context: REPO ROOT (needs API + Core + Infrastructure)
#     docker buildx build -f Agrovesto.Backend.API/Dockerfile -t nomaos-api:dev .
#
# Primary target: EC2 linux/amd64. linux/arm64 also builds, without QEMU:
# restore/publish run natively on the BUILDPLATFORM SDK and cross-publish with
# `dotnet publish -a <arch>`, so an arm64 image costs roughly the same as amd64
# instead of the 8-15x penalty of emulating the whole SDK.
#
#   docker buildx build --platform linux/amd64,linux/arm64 \
#     -f Agrovesto.Backend.API/Dockerfile -t $ECR/nomaos-api:sha-abc1234 --push .
#
# Hardening (CIS Docker / Checkov / Trivy):
#   * final stage runs as UID:GID 1654:1654, never root
#   * alpine runtime — no SDK, no shell tooling beyond curl, no git metadata
#   * only the publish output is copied in; no secrets, no source, no .git
#   * STOPSIGNAL SIGTERM + exec-form ENTRYPOINT so dotnet is PID 1 and drains
#   * HEALTHCHECK hits /health/live (self-check only — a SQL outage must never
#     make Docker restart-loop the container)
# ─────────────────────────────────────────────────────────────────────────────

ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:8.0-alpine
# Override to mcr.microsoft.com/dotnet/aspnet:8.0-jammy if a native dependency
# ever needs glibc (e.g. SkiaSharp's non-musl build used by QuestPDF).
ARG DOTNET_RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:8.0-alpine


########################################
# STAGE 1 — restore (native, never emulated)
#
# Only the .csproj files are copied first: the NuGet layer is then invalidated
# by a dependency change, not by every source edit.
########################################
FROM --platform=$BUILDPLATFORM ${DOTNET_SDK_IMAGE} AS restore

ARG TARGETARCH
WORKDIR /src

COPY Agrovesto.Backend.API/*.csproj            Agrovesto.Backend.API/
COPY Agrovesto.Backend.Core/*.csproj           Agrovesto.Backend.Core/
COPY Agrovesto.Backend.Infrastructure/*.csproj Agrovesto.Backend.Infrastructure/

# BuildKit cache mount: the package cache survives between builds and is never
# baked into a layer. sharing=locked avoids two concurrent builds corrupting it.
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet restore Agrovesto.Backend.API/Agrovesto.Backend.API.csproj \
      -a "$(case "$TARGETARCH" in amd64) echo x64 ;; arm64) echo arm64 ;; *) echo x64 ;; esac)"


########################################
# STAGE 2 — publish
########################################
FROM restore AS publish

ARG TARGETARCH
ARG COMMIT_SHA=unknown
ARG BUILD_VERSION=0.0.0
ARG BUILT_AT=unknown

COPY . .

# global.json pins the SDK to an exact patch with "rollForward": "disable" so
# every developer machine builds with the same compiler. Inside this image the
# base tag is the pin, and the SDK it ships is whatever 8.0.x patch Microsoft
# published most recently — with "disable" the build would fail with
# "compatible SDK version not found" for a reason that has nothing to do with
# the code. Relax the roll-forward policy for the container build only; the
# file on disk in git is untouched and TargetFramework=net8.0 still fixes the
# output.
RUN if [ -f global.json ]; then \
      sed -i 's/"rollForward"[[:space:]]*:[[:space:]]*"disable"/"rollForward": "latestMajor"/' global.json; \
    fi

RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet publish Agrovesto.Backend.API/Agrovesto.Backend.API.csproj \
      -c Release \
      -a "$(case "$TARGETARCH" in amd64) echo x64 ;; arm64) echo arm64 ;; *) echo x64 ;; esac)" \
      -o /app/publish \
      --no-restore \
      /p:UseAppHost=false \
      /p:EnableSourceControlManagerQueries=false \
      /p:InformationalVersion="${BUILD_VERSION}+${COMMIT_SHA}"

# Build stamps: /status and BuildInfo read .commit-sha when GIT_SHA is absent.
RUN printf '%s\n' "$COMMIT_SHA"    > /app/publish/.commit-sha \
 && printf '%s\n' "$BUILD_VERSION" > /app/publish/.version \
 && printf '%s\n' "$BUILT_AT"      > /app/publish/.deployed-at


########################################
# STAGE 3 — runtime
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

# icu + tzdata: the app formats Nigerian dates/currency, so invariant
# globalization is explicitly OFF and ICU must be present.
# curl: HEALTHCHECK and the deploy gate.
# libstdc++/libgcc/fontconfig/ttf-dejavu: native deps for PDF + image rendering.
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

# Fixed, non-root UID/GID. Numeric in USER so runAsNonRoot admission checks and
# `docker inspect` can verify it without resolving /etc/passwd.
RUN addgroup -g 1654 -S app \
 && adduser  -u 1654 -S -G app -h /app -s /sbin/nologin app

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_PRINT_TELEMETRY_MESSAGE=false \
    DOTNET_GCConserveMemory=5 \
    DOTNET_GCHeapHardLimitPercent=75 \
    TZ=UTC

COPY --from=publish --chown=1654:1654 /app/publish .

# Optional Serilog file sink target (LOG_FILE_PATH). Not a VOLUME: an anonymous
# volume per deploy would leak disk on every release.
RUN mkdir -p /app/logs && chown -R 1654:1654 /app/logs

EXPOSE 8080

# Liveness only. /health/ready would tie container restarts to SQL/Redis/Vault
# availability, turning a dependency blip into a fleet-wide restart storm.
HEALTHCHECK --interval=30s --timeout=5s --start-period=45s --retries=3 \
    CMD curl -fsS --max-time 4 http://127.0.0.1:8080/health/live || exit 1

# Graceful drain: exec form means dotnet is PID 1 and receives SIGTERM directly.
STOPSIGNAL SIGTERM

USER 1654:1654

ENTRYPOINT ["dotnet", "Agrovesto.Backend.API.dll"]
