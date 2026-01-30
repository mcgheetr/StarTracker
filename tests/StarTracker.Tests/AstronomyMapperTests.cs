namespace StarTracker.Tests;

public class AstronomyMapperTests
{
    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(45, -90, 270, 45)]
    [InlineData(-30, 190, 190, -30)]
    [InlineData(37.70443, -77.41832, 282.58168, 37.70443)]
    public void FromLatLon_ProducesExpectedRaDec(double lat, double lon, double expectedRa, double expectedDec)
    {
        var (ra, dec) = AstronomyMapper.FromLatLon(lat, lon);
        Assert.Equal(expectedRa, ra, 6);
        Assert.Equal(expectedDec, dec, 6);
    }
}