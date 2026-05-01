# ============================================================
# STAGE 1 — base runtime image (small + secure)
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS base

# Create non-root user early (better layer caching)
RUN useradd -m -u 10001 appuser

WORKDIR /app
EXPOSE 5000

# ASP.NET production defaults
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# ============================================================
# STAGE 2 — build
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build

WORKDIR /src

# ---- copy csproj first for better caching ----
COPY Agrovesto.Backend.API/*.csproj ./Agrovesto.Backend.API/
RUN dotnet restore Agrovesto.Backend.API/Agrovesto.Backend.API.csproj

# ---- copy remaining source ----
COPY . .

WORKDIR /src/Agrovesto.Backend.API

RUN dotnet build \
    -c Release \
    --no-restore

# ============================================================
# STAGE 3 — publish (trimmed for production)
# ============================================================
FROM build AS publish

RUN dotnet publish \
    -c Release \
    -o /app/publish \
    --no-build \
    /p:UseAppHost=false

# ============================================================
# STAGE 4 — final runtime image
# ============================================================
FROM base AS final

WORKDIR /app

# Copy published output
COPY --from=publish /app/publish .

# Fix permissions
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Optional container health check (safe default)
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD wget -qO- http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "Agrovesto.Backend.API.dll"]
