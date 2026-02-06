namespace StarTracker.Tests;

public class AstronomyMapperTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(45, -90)]
    [InlineData(-30, 190)]
    [InlineData(37.70443, -77.41832)]
    public void FromLatLon_ReturnsPolarisForDemo(double lat, double lon)
    {
        var (ra, dec) = AstronomyMapper.FromLatLon(lat, lon);
        Assert.Equal(37.954, ra, 6);
        Assert.Equal(89.264, dec, 6);
    }
}
