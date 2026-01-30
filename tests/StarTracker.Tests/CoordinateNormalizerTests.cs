namespace StarTracker.Tests;

public class CoordinateNormalizerTests
{
    [Theory]
    [InlineData("37.70443", 37.70443)]
    [InlineData("-77.41832", -77.41832)]
    [InlineData("37.7", 37.70000)]
    [InlineData("37.70443321", 37.70443)]
    [InlineData("37.704436", 37.70444)]
    [InlineData("37.70443N", 37.70443)]
    [InlineData("77.41832W", -77.41832)]
    public void TryParseDecimalDegrees_ParsesAndNormalizes(string input, double expected)
    {
        var ok = CoordinateNormalizer.TryParseDecimalDegrees(input, out var value);
        Assert.True(ok);
        Assert.Equal(expected, value, 5);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("37°42'15\"N")] // DMS not supported in decimal parser
    public void TryParseDecimalDegrees_InvalidInputs_ReturnFalse(string? input)
    {
        var ok = CoordinateNormalizer.TryParseDecimalDegrees(input, out var value);
        Assert.False(ok);
    }
}