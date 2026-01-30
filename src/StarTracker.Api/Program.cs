using StarTracker.Core;
using StarTracker.Core.Services;
using StarTracker.Infrastructure;


var builder = WebApplication.CreateBuilder(args);

// Data Protection for dev encryption of sensitive payloads
builder.Services.AddDataProtection();

// Configure encryption provider via configuration (default: DataProtection). To enable AWS Encryption SDK provider set "Encryption:UseAwsSdk" = "true" and provide KMS key/credentials.
var useAwsSdk = builder.Configuration.GetValue<bool>("Encryption:UseAwsSdk");
if (useAwsSdk)
{
    // Register fake envelope encryptor for now (replace with real SDK-backed implementation when enabled)
    builder.Services.AddSingleton<IAwsEnvelopeEncryptor, FakeAwsEnvelopeEncryptor>();
    builder.Services.AddSingleton<IEncryptionService, KmsEncryptionService>();
}
else
{
    builder.Services.AddSingleton<IEncryptionService, DataProtectionEncryptionService>();
}

// Guidance service
builder.Services.AddSingleton<IGuidanceService, SimpleGuidanceService>();

builder.Services.AddSingleton<IObservationRepository, InMemoryObservationRepository>();

var app = builder.Build();

// Simple API key middleware - reads required key from configuration ("ApiKey"). If not configured, any non-empty header is accepted.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var key = context.Request.Headers[Constants.ApiKeyHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(Results.Problem(detail: $"Missing {Constants.ApiKeyHeader} header", statusCode: 401));
            return;
        }

        var required = builder.Configuration["ApiKey"];
        if (!string.IsNullOrWhiteSpace(required) && !string.Equals(required, key, StringComparison.Ordinal))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(Results.Problem(detail: "Invalid API key", statusCode: 401));
            return;
        }
    }

    await next();
});

app.MapGet("/api/v1/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/api/v1/stars/{target}/position", (string target, string? lat, string? lon, DateTimeOffset? at) =>
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

    // Use the guidance service to produce human-friendly text
    var guidanceSvc = app.Services.GetRequiredService<IGuidanceService>();
    var guidance = guidanceSvc.GenerateGuidance(az, alt, target);

    var resp = new PositionResponseDto(target, ra, dec, az, alt, guidance, timestamp);
    return Results.Ok(resp);
});

app.MapPost("/api/v1/stars/{target}/observations", async (string target, CreateObservationRequest req, IObservationRepository repo, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(target))
        return Results.Problem(detail: "target is required", statusCode: 400);
    if (string.IsNullOrWhiteSpace(req.Observer))
        return Results.Problem(detail: "Observer is required", statusCode: 400);
    if (req.RightAscensionDegrees < 0 || req.RightAscensionDegrees >= 360)
        return Results.Problem(detail: "RightAscensionDegrees must be in [0, 360)", statusCode: 400);
    if (req.DeclinationDegrees < -90 || req.DeclinationDegrees > 90)
        return Results.Problem(detail: "DeclinationDegrees must be between -90 and 90", statusCode: 400);

    var dto = new ObservationDto(System.Guid.NewGuid(), target, req.ObservedAt, req.RightAscensionDegrees, req.DeclinationDegrees, req.Observer, req.Notes);
    var saved = await repo.CreateObservationAsync(dto, ct);
    return Results.Created($"/api/v1/observations/{saved.Id}", saved);
});

app.MapGet("/api/v1/stars/{target}/observations", async (string target, DateTimeOffset? from, DateTimeOffset? to, IObservationRepository repo, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(target))
        return Results.Problem(detail: "target is required", statusCode: 400);

    var items = await repo.GetObservationsAsync(target, from, to, ct);
    return Results.Ok(items);
});

app.MapGet("/api/v1/observations/{id}", async (string id, IObservationRepository repo, CancellationToken ct) =>
{
    if (!System.Guid.TryParse(id, out var guid))
        return Results.Problem(detail: "invalid id", statusCode: 400);

    var obs = await repo.GetObservationAsync(guid, ct);
    return obs is null ? Results.NotFound() : Results.Ok(obs);
});

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }
