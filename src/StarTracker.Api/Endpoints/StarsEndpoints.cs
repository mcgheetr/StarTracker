using StarTracker.Core;
using StarTracker.Core.Services;

namespace StarTracker.Api.Endpoints;

public static class StarsEndpoints
{
    public static void MapStarTrackerEndpoints(this WebApplication app)
    {
        // Health check
        app.MapGet("/api/v1/health", Health)
            .WithName("Health");

        // Get star position
        app.MapGet("/api/v1/stars/{target}/position", GetStarPosition)
            .WithName("Get Star Position");

        // Create observation
        app.MapPost("/api/v1/stars/{target}/observations", CreateObservation)
            .WithName("Create Observation");

        // Get observations for a target
        app.MapGet("/api/v1/stars/{target}/observations", GetObservations)
            .WithName("Get Observations");

        // Get observation by ID
        app.MapGet("/api/v1/observations/{id}", GetObservationById)
            .WithName("Get Observation");
    }

    // Handlers
    private static IResult Health()
    {
        return Results.Ok(new { status = "healthy" });
    }

    private static IResult GetStarPosition(
        string target,
        string? lat,
        string? lon,
        DateTimeOffset? at,
        IGuidanceService guidanceSvc)
    {
        if (string.IsNullOrWhiteSpace(target))
            return Results.Problem(detail: "target is required", statusCode: 400);

        if (!CoordinateNormalizer.TryParseDecimalDegrees(lat, out var nlat))
            return Results.Problem(detail: "lat query parameter is required and must be decimal degrees", statusCode: 400);
        if (!CoordinateNormalizer.TryParseDecimalDegrees(lon, out var nlon))
            return Results.Problem(detail: "lon query parameter is required and must be decimal degrees", statusCode: 400);

        if (nlat < -90 || nlat > 90)
            return Results.Problem(detail: "lat must be between -90 and 90", statusCode: 400);
        if (nlon < -180 || nlon > 180)
            return Results.Problem(detail: "lon must be between -180 and 180", statusCode: 400);

        var timestamp = at ?? DateTimeOffset.UtcNow;

        var (ra, dec) = AstronomyMapper.FromLatLon(nlat, nlon);
        var (az, alt) = AstronomyMapper.ComputeAzAlt(nlat, nlon, ra, dec, timestamp);

        // Use guidance service for human-friendly text
        var guidance = guidanceSvc.GenerateGuidance(az, alt, target);

        var resp = new PositionResponseDto(target, ra, dec, az, alt, guidance, timestamp);
        return Results.Ok(resp);
    }

    private static async Task<IResult> CreateObservation(
        string target,
        CreateObservationRequest req,
        IObservationRepository repo,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(target))
            return Results.Problem(detail: "target is required", statusCode: 400);
        if (string.IsNullOrWhiteSpace(req.Observer))
            return Results.Problem(detail: "Observer is required", statusCode: 400);
        if (req.RightAscensionDegrees < 0 || req.RightAscensionDegrees >= 360)
            return Results.Problem(detail: "RightAscensionDegrees must be in [0, 360)", statusCode: 400);
        if (req.DeclinationDegrees < -90 || req.DeclinationDegrees > 90)
            return Results.Problem(detail: "DeclinationDegrees must be between -90 and 90", statusCode: 400);

        var dto = new ObservationDto(Guid.NewGuid(), target, req.ObservedAt, req.RightAscensionDegrees, req.DeclinationDegrees, req.Observer, req.Notes);
        var saved = await repo.CreateObservationAsync(dto, ct);
        return Results.Created($"/api/v1/observations/{saved.Id}", saved);
    }

    private static async Task<IResult> GetObservations(
        string target,
        DateTimeOffset? from,
        DateTimeOffset? to,
        IObservationRepository repo,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(target))
            return Results.Problem(detail: "target is required", statusCode: 400);

        var items = await repo.GetObservationsAsync(target, from, to, ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetObservationById(string id, IObservationRepository repo, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var guid))
            return Results.Problem(detail: "invalid id", statusCode: 400);

        var obs = await repo.GetObservationAsync(guid, ct);
        return obs is null ? Results.NotFound() : Results.Ok(obs);
    }
}
