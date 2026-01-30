namespace StarTracker.Core.Interfaces;

public interface IEncryptionService
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}