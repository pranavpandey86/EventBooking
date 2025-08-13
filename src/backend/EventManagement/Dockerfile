# EventManagement API Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create non-root user for security
RUN addgroup --system --gid 1001 dotnet \
    && adduser --system --uid 1001 --ingroup dotnet dotnet

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for dependency resolution
COPY ["EventManagement.API/EventManagement.API.csproj", "EventManagement.API/"]
COPY ["EventManagement.Core/EventManagement.Core.csproj", "EventManagement.Core/"]
COPY ["EventManagement.Infrastructure/EventManagement.Infrastructure.csproj", "EventManagement.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "EventManagement.API/EventManagement.API.csproj"

# Copy all source code
COPY . .
WORKDIR "/src/EventManagement.API"

# Build application
RUN dotnet build "EventManagement.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EventManagement.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R dotnet:dotnet /app
USER dotnet

# Health check endpoint
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "EventManagement.API.dll"]
