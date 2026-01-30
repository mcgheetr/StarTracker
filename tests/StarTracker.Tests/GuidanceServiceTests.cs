namespace StarTracker.Tests;

public class GuidanceServiceTests
{
    [Theory]
    [InlineData(0, 45, "Polaris")]
    [InlineData(10, 40, "Polaris")]
    [InlineData(200, 30, "Vega")]
    public void GenerateGuidance_ProducesReadableText(double az, double alt, string target)
    {
        var svc = new SimpleGuidanceService();
        var text = svc.GenerateGuidance(az, alt, target);
        Assert.NotNull(text);
        Assert.Contains(target, text, StringComparison.OrdinalIgnoreCase);
    }
}