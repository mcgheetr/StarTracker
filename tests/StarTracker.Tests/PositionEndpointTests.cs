using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace StarTracker.Tests;

public class PositionEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task GetPosition_ReturnsExpectedRaDec_ForPolaris()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "test");

        var response = await client.GetAsync("/api/v1/stars/Polaris/position?lat=37.70443&lon=-77.41832&at=2026-01-29T16:00:00Z");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pos = await response.Content.ReadFromJsonAsync<PositionResponseDto>();
        Assert.NotNull(pos);
        Assert.Equal("Polaris", pos!.Target);
        Assert.Equal(89.2641, pos.DeclinationDegrees, 4);
        Assert.Equal(37.9546, pos.RightAscensionDegrees, 4);
        Assert.InRange(pos.AzimuthDegrees, 0, 360);
        Assert.InRange(pos.AltitudeDegrees, -90, 90);
        Assert.Contains("Polaris", pos.Guidance, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("alpha UMi")]
    [InlineData("north star")]
    [InlineData("HIP 11767")]
    public async Task GetPosition_AcceptsAliases_AndCanonicalizesTarget(string alias)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "test");

        var response = await client.GetAsync($"/api/v1/stars/{Uri.EscapeDataString(alias)}/position?lat=37.70443&lon=-77.41832&at=2026-01-29T16:00:00Z");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pos = await response.Content.ReadFromJsonAsync<PositionResponseDto>();
        Assert.NotNull(pos);
        Assert.Equal("Polaris", pos!.Target);
    }

    [Fact]
    public async Task GetPosition_ReturnsNotFound_ForUnknownTarget()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "test");

        var response = await client.GetAsync("/api/v1/stars/UnknownStar/position?lat=37.70443&lon=-77.41832&at=2026-01-29T16:00:00Z");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPosition_IsIdempotent_ForRepeatedSameRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "test");

        const string url = "/api/v1/stars/Vega/position?lat=37.70443&lon=-77.41832&at=2026-01-29T16:00:00Z";

        var first = await client.GetFromJsonAsync<PositionResponseDto>(url);
        var second = await client.GetFromJsonAsync<PositionResponseDto>(url);
        var third = await client.GetFromJsonAsync<PositionResponseDto>(url);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotNull(third);

        const double raDecTolerance = 0.0005;
        const double azAltTolerance = 0.001;

        Assert.InRange(Math.Abs(first!.RightAscensionDegrees - second!.RightAscensionDegrees), 0, raDecTolerance);
        Assert.InRange(Math.Abs(first.DeclinationDegrees - second.DeclinationDegrees), 0, raDecTolerance);
        Assert.InRange(Math.Abs(first.AzimuthDegrees - second.AzimuthDegrees), 0, azAltTolerance);
        Assert.InRange(Math.Abs(first.AltitudeDegrees - second.AltitudeDegrees), 0, azAltTolerance);

        Assert.InRange(Math.Abs(second.RightAscensionDegrees - third!.RightAscensionDegrees), 0, raDecTolerance);
        Assert.InRange(Math.Abs(second.DeclinationDegrees - third.DeclinationDegrees), 0, raDecTolerance);
        Assert.InRange(Math.Abs(second.AzimuthDegrees - third.AzimuthDegrees), 0, azAltTolerance);
        Assert.InRange(Math.Abs(second.AltitudeDegrees - third.AltitudeDegrees), 0, azAltTolerance);
    }
}
