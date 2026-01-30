using System;
using System.Text;
using StarTracker.Core.Interfaces;

namespace StarTracker.Infrastructure;

// Mock/placeholder for the AWS Encryption SDK-backed encryptor. This class simulates
// SDK behavior for local testing. When ready, replace internals with real AWS.Cryptography.EncryptionSDK calls.
public class AwsEncryptionSdkService : IEncryptionService
{
    private readonly string _keyId;

    public AwsEncryptionSdkService(string keyId)
    {
        _keyId = keyId ?? throw new ArgumentNullException(nameof(keyId));
    }

    public string Protect(string plaintext)
    {
        if (plaintext is null) throw new ArgumentNullException(nameof(plaintext));
        // Simulate SDK envelope encryption by encoding with key id metadata
        var payload = Encoding.UTF8.GetBytes(plaintext);
        var b64 = Convert.ToBase64String(payload);
        return $"SDK|{_keyId}|{b64}";
    }

    public string Unprotect(string ciphertext)
    {
        if (ciphertext is null) throw new ArgumentNullException(nameof(ciphertext));
        if (!ciphertext.StartsWith("SDK|")) throw new InvalidOperationException("Invalid SDK envelope payload");
        var parts = ciphertext.Split('|', 3);
        if (parts.Length != 3) throw new InvalidOperationException("Invalid SDK envelope payload");
        var b64 = parts[2];
        var bytes = Convert.FromBase64String(b64);
        return Encoding.UTF8.GetString(bytes);
    }
}