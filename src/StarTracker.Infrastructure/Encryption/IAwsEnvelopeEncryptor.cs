namespace StarTracker.Infrastructure.Encryption;

public interface IAwsEnvelopeEncryptor
{
    // Return serialized envelope string
    string Protect(string plaintext);
    string Unprotect(string envelope);
}
