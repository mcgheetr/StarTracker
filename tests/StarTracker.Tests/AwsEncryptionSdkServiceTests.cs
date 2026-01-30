namespace StarTracker.Tests;

public class AwsEncryptionSdkServiceTests
{
    [Fact]
    public void MockSdk_RoundTrip()
    {
        var svc = new StarTracker.Infrastructure.AwsEncryptionSdkService("test-key");
        var clear = "secret-payload";
        var ct = svc.Protect(clear);
        Assert.NotNull(ct);
        Assert.NotEqual(clear, ct);

        var recovered = svc.Unprotect(ct);
        Assert.Equal(clear, recovered);
    }
}