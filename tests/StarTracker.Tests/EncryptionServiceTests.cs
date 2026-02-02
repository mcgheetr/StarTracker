using StarTracker.Infrastructure.Encryption;

namespace StarTracker.Tests;

public class EncryptionServiceTests
{
    [Fact]
    public void DataProtection_RoundTrip()
    {
        var provider = DataProtectionProvider.Create("StarTracker.Tests");
        var svc = new DataProtectionEncryptionService(provider);

        var clear = "{\"RightAscensionDegrees\":282.58168,\"DeclinationDegrees\":37.70443}";
        var ct = svc.Protect(clear);
        Assert.NotNull(ct);
        Assert.NotEqual(clear, ct);

        var recovered = svc.Unprotect(ct);
        Assert.Equal(clear, recovered);
    }
}