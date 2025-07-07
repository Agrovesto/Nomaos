# Use the ARM64-compatible .NET SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["Agrovesto.Backend.API/Agrovesto.Backend.API.csproj", "Agrovesto.Backend.API/"]
RUN dotnet restore "Agrovesto.Backend.API/Agrovesto.Backend.API.csproj"

# Copy the rest of the application code
COPY . .

# Build the application
WORKDIR "/src/Agrovesto.Backend.API"
RUN dotnet build "Agrovesto.Backend.API.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "Agrovesto.Backend.API.csproj" -c Release -o /app/publish

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Expose the port the app will run on
EXPOSE 5000

# Set the entry point for the application
ENTRYPOINT ["dotnet", "Agrovesto.Backend.API.dll"]
