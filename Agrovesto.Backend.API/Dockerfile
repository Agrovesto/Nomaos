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

# NOTE: no `--mount=type=cache` here on purpose. Cache-mount contents are NOT
# part of the image layer, so the later `publish --no-restore` stage would not
# find the restored packages. Keeping packages in this layer lets the GHA
# layer cache (cache-from/cache-to) do the caching instead.
RUN echo "=== .NET SDK ===" \
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

WORKDIR /src

# Copy the complete source only after restore.
# Requires a .dockerignore that excludes **/bin and **/obj, otherwise host
# obj/project.assets.json files overwrite the ones created by restore.
COPY . .

# Build metadata is declared late (right before it is used) so that changing
# a commit SHA / run URL does not bust the layers above.
ARG COMMIT_SHA=unknown
ARG BUILD_VERSION=0.0.0

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

# Build + git metadata shipped inside the image.
ARG BUILT_AT=unknown
ARG GIT_BRANCH=unknown
ARG BUILD_RUN_URL=
ARG BUILD_ACTOR=
ARG PR_NUMBER=
ARG PR_AUTHOR=
ARG PR_APPROVERS=

RUN printf '%s\n' "$COMMIT_SHA" \
      > /app/publish/.commit-sha \
 && printf '%s\n' "$BUILD_VERSION" \
      > /app/publish/.version \
 && printf '%s\n' "$BUILT_AT" \
      > /app/publish/.deployed-at \
 && printf '{\n  "commit": "%s",\n  "branch": "%s",\n  "version": "%s",\n  "builtAt": "%s",\n  "runUrl": "%s",\n  "actor": "%s",\n  "pullRequest": {\n    "number": "%s",\n    "author": "%s",\n    "approvers": "%s"\n  }\n}\n' \
      "$COMMIT_SHA" "$GIT_BRANCH" "$BUILD_VERSION" "$BUILT_AT" \
      "$BUILD_RUN_URL" "$BUILD_ACTOR" \
      "$PR_NUMBER" "$PR_AUTHOR" "$PR_APPROVERS" \
      > /app/publish/build-info.json


########################################
# STAGE 3 — RUNTIME
########################################

FROM ${DOTNET_RUNTIME_IMAGE} AS runtime

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
# Git / build metadata
#
# Declared after the dependency layers above so a new commit only
# invalidates the layers from here down.
########################################

ARG COMMIT_SHA=unknown
ARG BUILD_VERSION=0.0.0
ARG BUILT_AT=unknown
ARG GIT_BRANCH=unknown
ARG BUILD_RUN_URL=
ARG BUILD_ACTOR=
ARG PR_NUMBER=
ARG PR_AUTHOR=
ARG PR_APPROVERS=

# The app reads GIT_SHA / COMMIT_SHA for the Serilog `git.sha` property.
ENV GIT_SHA=$COMMIT_SHA \
    COMMIT_SHA=$COMMIT_SHA \
    GIT_BRANCH=$GIT_BRANCH \
    BUILD_VERSION=$BUILD_VERSION \
    BUILT_AT=$BUILT_AT

LABEL org.opencontainers.image.title="nomaos-onboarding-api" \
      org.opencontainers.image.description="NomaOS Onboarding API (ASP.NET Core 8)" \
      org.opencontainers.image.source="https://github.com/Agrovesto/Nomaos" \
      org.opencontainers.image.revision="$COMMIT_SHA" \
      org.opencontainers.image.version="$BUILD_VERSION" \
      org.opencontainers.image.created="$BUILT_AT" \
      org.opencontainers.image.ref.name="$GIT_BRANCH" \
      org.opencontainers.image.vendor="Agrovesto" \
      io.nomaos.build.run-url="$BUILD_RUN_URL" \
      io.nomaos.build.actor="$BUILD_ACTOR" \
      io.nomaos.pr.number="$PR_NUMBER" \
      io.nomaos.pr.author="$PR_AUTHOR" \
      io.nomaos.pr.approvers="$PR_APPROVERS"

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