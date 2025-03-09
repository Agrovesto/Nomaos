# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-nanoserver-1809 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-nanoserver-1809 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Agrovesto.Backend.API/Agrovesto.Backend.API.csproj", "Agrovesto.Backend.API/"]
RUN setx ContainerUser "Administrator"
RUN dotnet restore "./Agrovesto.Backend.API/Agrovesto.Backend.API.csproj"
COPY . .
WORKDIR "/src/Agrovesto.Backend.API"
RUN dotnet build "./Agrovesto.Backend.API.csproj" -c %BUILD_CONFIGURATION% -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Agrovesto.Backend.API.csproj" -c %BUILD_CONFIGURATION% -o /app/publish /p:UseAppHost=false

# Final runtime stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Agrovesto.Backend.API.dll"]
