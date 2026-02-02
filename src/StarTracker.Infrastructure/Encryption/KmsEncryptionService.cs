using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;

namespace StarTracker.Infrastructure.Encryption;

public class KmsEncryptionService : IEncryptionService
{
    private readonly IAwsEnvelopeEncryptor _envelope;

    public KmsEncryptionService(IAwsEnvelopeEncryptor envelope)
    {
        _envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
    }

    public string Protect(string plaintext)
    {
        return _envelope.Protect(plaintext);
    }

    public string Unprotect(string ciphertext)
    {
        return _envelope.Unprotect(ciphertext);
    }
}
