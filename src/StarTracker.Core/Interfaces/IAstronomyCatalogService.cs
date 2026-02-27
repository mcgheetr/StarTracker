using StarTracker.Core.DTOs;

namespace StarTracker.Core.Interfaces;

public interface IAstronomyCatalogService
{
    Task<AstronomyPositionResult?> GetPositionAsync(
        string target,
        double observerLat,
        double observerLon,
        DateTimeOffset at,
        CancellationToken ct);
}
