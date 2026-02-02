using Microsoft.AspNetCore.DataProtection;

namespace StarTracker.Infrastructure.Encryption;

public class DataProtectionEncryptionService(IDataProtectionProvider provider) : IEncryptionService
{
    private readonly IDataProtector _protector = provider.CreateProtector("StarTracker.Location");

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
