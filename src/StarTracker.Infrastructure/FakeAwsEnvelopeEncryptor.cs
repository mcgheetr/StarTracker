using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace StarTracker.Infrastructure;

// Deterministic, test-only fake that simulates envelope encryption using AES-GCM under the hood.
public class FakeAwsEnvelopeEncryptor : IAwsEnvelopeEncryptor
{
    public string Protect(string plaintext)
    {
        var key = RandomNumberGenerator.GetBytes(32);
using var aes = new System.Security.Cryptography.AesGcm(key, 16);
        var pt = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ct = new byte[pt.Length];
        var tag = new byte[16];
        aes.Encrypt(nonce, pt, ct, tag, null);

        // For the "encryptedKey" in fake, just base64 the key XORed with 0xAA — fake reversible transform
        var fakeEncryptedKey = Convert.ToBase64String(key.Select(b => (byte)(b ^ 0xAA)).ToArray());

        var envelope = new
        {
            encryptedKey = fakeEncryptedKey,
            nonce = Convert.ToBase64String(nonce),
            ciphertext = Convert.ToBase64String(ct),
            tag = Convert.ToBase64String(tag)
        };

        return JsonSerializer.Serialize(envelope);
    }

    public string Unprotect(string envelope)
    {
        var doc = JsonDocument.Parse(envelope);
        var root = doc.RootElement;
        var encKey = Convert.FromBase64String(root.GetProperty("encryptedKey").GetString()!);
        var nonce = Convert.FromBase64String(root.GetProperty("nonce").GetString()!);
        var ct = Convert.FromBase64String(root.GetProperty("ciphertext").GetString()!);
        var tag = Convert.FromBase64String(root.GetProperty("tag").GetString()!);

        // Reverse fake transform
        var key = encKey.Select(b => (byte)(b ^ 0xAA)).ToArray();

        using var aes = new System.Security.Cryptography.AesGcm(key, 16);
        var pt = new byte[ct.Length];
        aes.Decrypt(nonce, ct, tag, pt, null);
        return Encoding.UTF8.GetString(pt);
    }
}