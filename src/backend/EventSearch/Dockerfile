# Use the official .NET 9 SDK image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set the working directory
WORKDIR /src

# Copy solution file
COPY EventSearch.sln ./

# Copy project files
COPY EventSearch.API/EventSearch.API.csproj EventSearch.API/
COPY EventSearch.Core/EventSearch.Core.csproj EventSearch.Core/
COPY EventSearch.Infrastructure/EventSearch.Infrastructure.csproj EventSearch.Infrastructure/
COPY EventSearch.Tests/EventSearch.Tests.csproj EventSearch.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY EventSearch.API/ EventSearch.API/
COPY EventSearch.Core/ EventSearch.Core/
COPY EventSearch.Infrastructure/ EventSearch.Infrastructure/

# Build the application
RUN dotnet build EventSearch.API/EventSearch.API.csproj -c Release -o /app/build

# Publish the application
RUN dotnet publish EventSearch.API/EventSearch.API.csproj -c Release -o /app/publish

# Use the official .NET 9 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Set the working directory
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Create a non-root user and set ownership
RUN groupadd -r appuser && useradd -r -g appuser appuser && \
    chown -R appuser:appuser /app
USER appuser

# Set the entry point
ENTRYPOINT ["dotnet", "EventSearch.API.dll"]