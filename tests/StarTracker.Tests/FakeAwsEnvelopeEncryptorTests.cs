namespace StarTracker.Tests;

public class FakeAwsEnvelopeEncryptorTests
{
    [Fact]
    public void RoundTrip_Works()
    {
        var svc = new StarTracker.Infrastructure.FakeAwsEnvelopeEncryptor();
        var clear = "{" + "\"RightAscensionDegrees\":282.58168,\"DeclinationDegrees\":37.70443}";
        var ct = svc.Protect(clear);
        Assert.NotNull(ct);
        Assert.NotEqual(clear, ct);

        var recovered = svc.Unprotect(ct);
        Assert.Equal(clear, recovered);
    }
}