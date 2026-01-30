namespace StarTracker.Tests;

public class KmsEncryptionServiceTests
{
    [Fact]
    public void KmsEnvelope_RoundTrip_With_FakeEnvelope()
    {
        var fake = new StarTracker.Infrastructure.FakeAwsEnvelopeEncryptor();
        var svc = new KmsEncryptionService(fake);

        var clear = "hello secret coords";
        var ct = svc.Protect(clear);
        Assert.NotNull(ct);
        Assert.NotEqual(clear, ct);

        var recovered = svc.Unprotect(ct);
        Assert.Equal(clear, recovered);
    }
}