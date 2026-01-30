namespace StarTracker.Infrastructure;

public interface IAwsEnvelopeEncryptor
{
    // Return serialized envelope string
    string Protect(string plaintext);
    string Unprotect(string envelope);
}