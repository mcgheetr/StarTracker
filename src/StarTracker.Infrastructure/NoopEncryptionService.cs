namespace StarTracker.Infrastructure;

public class NoopEncryptionService : IEncryptionService
{
    public string Protect(string plaintext) => plaintext;
    public string Unprotect(string ciphertext) => ciphertext;
}