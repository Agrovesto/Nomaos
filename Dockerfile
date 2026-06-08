# syntax=docker/dockerfile:1.7

########################################
# STAGE 1 — dependency restore
# Isolated so source changes don't bust
# the NuGet layer cache.
########################################
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS restore

WORKDIR /src

# Only project files — cache is invalidated only when
# a .csproj / .sln changes, not on every source edit.
COPY Agrovesto.Backend.sln .
COPY Agrovesto.Backend.API/*.csproj                Agrovesto.Backend.API/
COPY Agrovesto.Backend.Core/*.csproj               Agrovesto.Backend.Core/
COPY Agrovesto.Backend.Infrastructure/*.csproj     Agrovesto.Backend.Infrastructure/

RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet restore Agrovesto.Backend.sln \
      --runtime linux-x64 \
      --locked-mode \
      /p:EnableSourceLink=false \
      /p:EnableSourceControlManagerQueries=false


########################################
# STAGE 2 — build + publish
# Separate from restore so the NuGet
# cache layer is never dirtied by source.
########################################
FROM restore AS publish

# Copy full source on top of the already-restored tree
COPY . .

RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet publish Agrovesto.Backend.API/Agrovesto.Backend.API.csproj \
      --no-restore \
      -c Release \
      --runtime linux-x64 \
      --self-contained false \
      -o /app/publish \
      /p:UseAppHost=false \
      /p:PublishReadyToRun=true \
      /p:PublishTrimmed=false \
      /p:ContinuousIntegrationBuild=true \
      /p:EnableSourceLink=false \
      /p:EnableSourceControlManagerQueries=false \
      /p:Deterministic=true \
      /p:PathMap=/src=. \
      /p:DebugType=none \
      /p:DebugSymbols=false


########################################
# STAGE 3 — runtime (minimal + hardened)
########################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS runtime

# ── Security: non-root by default in chiseled; make it explicit ──
# chiseled images already run as UID 1654 (app), no USER line needed,
# but we document it clearly.
# USER app  ← already set by the base image

WORKDIR /app

# ── Fail fast if required env vars are missing at runtime ──
# These are documentation + validation anchors; real values
# come from ECS task def / docker run / Kubernetes secrets.
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_RUNNING_IN_CONTAINER=true \
    # Tune the GC for container memory limits
    DOTNET_GCConserveMemory=5 \
    DOTNET_GCHeapHardLimitPercent=75 \
    # ThreadPool — prevents cold-start latency on first burst
    DOTNET_ThreadPool_UnfairSemaphoreSpinLimit=6

# ── Healthcheck (Docker / ECS / compose native) ──
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:5000/health || exit 1

COPY --from=publish /app/publish .

EXPOSE 5000

# Use array form — no shell spawned, signals go directly to dotnet
ENTRYPOINT ["dotnet", "Agrovesto.Backend.API.dll"]