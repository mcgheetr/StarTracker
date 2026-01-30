using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace StarTracker.Tests;

public class PositionEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task GetPosition_ReturnsExpectedRaDec_ForGivenLatLon()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "test");

        var response = await client.GetAsync("/api/v1/stars/Polaris/position?lat=37.70443&lon=-77.41832&at=2026-01-29T16:00:00Z");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pos = await response.Content.ReadFromJsonAsync<PositionResponseDto>();
        Assert.NotNull(pos);
        Assert.Equal("Polaris", pos!.Target);
        Assert.Equal(37.70443, pos.DeclinationDegrees, 6);
        Assert.Equal(282.58168, pos.RightAscensionDegrees, 5);
        Assert.InRange(pos.AzimuthDegrees, 0, 360);
        Assert.InRange(pos.AltitudeDegrees, -90, 90);
        Assert.Contains("Polaris", pos.Guidance, StringComparison.OrdinalIgnoreCase);

        // Now test fewer decimals are padded (lat=37.7 -> stored/returned as 37.70000 when rounded to 5 decimals)
        var response2 = await client.GetAsync("/api/v1/stars/Polaris/position?lat=37.7&lon=-77.4&at=2026-01-29T16:00:00Z");
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var pos2 = await response2.Content.ReadFromJsonAsync<PositionResponseDto>();
        Assert.NotNull(pos2);
        Assert.Equal(37.70000, Math.Round(pos2!.DeclinationDegrees, 5));
        Assert.Equal(282.60000, Math.Round(pos2.RightAscensionDegrees, 5));
        Assert.InRange(pos2.AzimuthDegrees, 0, 360);
        Assert.InRange(pos2.AltitudeDegrees, -90, 90);

    }
}