# ==============================================================================
# Multi-Stage Dockerfile for ASP.NET Core 8 Web API (Bookism)
# Best practices: Minimal footprint, cached layers, and non-root user execution
# ==============================================================================

# ------------------------------------------------------------------------------
# Stage 1: Build & Publish
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# 1. Copy project definition and restore dependencies as distinct layer (cache optimization)
COPY bookism.csproj ./
RUN dotnet restore bookism.csproj

# 2. Copy the remaining source files and compile
COPY . ./
RUN dotnet publish bookism.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ------------------------------------------------------------------------------
# Stage 2: Ephemeral Migration Runner (used by docker-compose migration service)
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS migrator
WORKDIR /src
RUN dotnet tool install --global dotnet-ef --version 8.0.8
ENV PATH="$PATH:/root/.dotnet/tools"
COPY --from=build /src ./
ENTRYPOINT ["dotnet-ef", "database", "update"]

# ------------------------------------------------------------------------------
# Stage 3: Minimal Production Runtime
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Install curl for reliable health checks inside the container
RUN apk add --no-cache curl

# Configure default container environment
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

# Copy published binaries from build stage
COPY --from=build /app/publish ./

# Switch to standard non-root user 'app' (UID 1654 provided in .NET 8 images)
USER app

EXPOSE 8080

# Health check targeting internal readiness endpoint
HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health/ready || exit 1

ENTRYPOINT ["dotnet", "bookism.dll"]
