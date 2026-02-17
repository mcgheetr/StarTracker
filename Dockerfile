# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY StarTracker.sln .
COPY src/StarTracker.Api/StarTracker.Api.csproj src/StarTracker.Api/
COPY src/StarTracker.Core/StarTracker.Core.csproj src/StarTracker.Core/
COPY src/StarTracker.Infrastructure/StarTracker.Infrastructure.csproj src/StarTracker.Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/

# Build and publish
WORKDIR /src/src/StarTracker.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN addgroup --system --gid 1000 appuser && \
    adduser --system --uid 1000 --ingroup appuser appuser

# Copy published app
COPY --from=build /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/api/v1/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "StarTracker.Api.dll"]
