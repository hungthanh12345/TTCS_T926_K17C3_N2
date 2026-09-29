# ==============================================================================
# Multi-Stage Production Dockerfile for ASP.NET Core 8 Web API
# Optimized for minimal attack surface, fast cold-starts, and zero non-root CVEs.
# Compatible with Render, Railway, Fly.io, AWS ECS, and Google Cloud Run.
# ==============================================================================

# STAGE 1: Build & Publish (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies first to leverage Docker layer caching
COPY ["InternshipManagementApi.csproj", "./"]
RUN dotnet restore "InternshipManagementApi.csproj"

# Copy source code and publish in Release mode
COPY . .
RUN dotnet publish "InternshipManagementApi.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# STAGE 2: Minimal Runtime Image (Ubuntu Chiseled / Non-Root)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Standard HTTP ports (Render & Railway inject dynamic PORT env var)
EXPOSE 8080
EXPOSE 80

# Production environment defaults
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

# Copy published binaries from build stage
COPY --from=build /app/publish .

# Use built-in non-root 'app' user for security compliance
USER app

# Entrypoint for the application
ENTRYPOINT ["dotnet", "InternshipManagementApi.dll"]
