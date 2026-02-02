using System;
using StarTracker.Core.Interfaces;

namespace StarTracker.Infrastructure.Encryption;

/// <summary>
/// Placeholder for the real AWS Encryption SDK envelope encryptor implementation.
/// When live integration is enabled, replace the internals with calls to
/// AWS.Cryptography.EncryptionSDK APIs (keyrings + encrypt/decrypt operations).
/// For now this throws NotImplementedException to avoid accidental use.
/// </summary>
public class AwsSdkEnvelopeEncryptor : IAwsEnvelopeEncryptor
{
    private readonly string _kmsKeyId;

    public AwsSdkEnvelopeEncryptor(string kmsKeyId)
    {
        _kmsKeyId = kmsKeyId ?? throw new ArgumentNullException(nameof(kmsKeyId));
    }

    public string Protect(string plaintext)
    {
        throw new NotImplementedException("AWS Encryption SDK integration not implemented. Configure FakeAwsEnvelopeEncryptor for local testing.");
    }

    public string Unprotect(string envelope)
    {
        throw new NotImplementedException("AWS Encryption SDK integration not implemented. Configure FakeAwsEnvelopeEncryptor for local testing.");
    }
}
