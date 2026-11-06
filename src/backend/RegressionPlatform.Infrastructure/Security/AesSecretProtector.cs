using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using RegressionPlatform.Application.Abstractions;

namespace RegressionPlatform.Infrastructure.Security;

/// <summary>
/// AES-GCM authenticated encryption for secrets at rest. The 256-bit key is derived (SHA-256)
/// from the configured <c>Encryption:Key</c>. Output layout: nonce(12) | tag(16) | ciphertext.
/// </summary>
public sealed class AesSecretProtector : ISecretProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesSecretProtector(IConfiguration configuration)
    {
        var configured =
            configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption key 'Encryption:Key' is not configured.");

        // Derive a stable 32-byte (256-bit) key from the configured secret.
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
    }

    public byte[] Encrypt(string plaintext)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipher, tag);

        var output = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, output, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, output, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, output, NonceSize + TagSize, cipher.Length);
        return output;
    }

    public string Decrypt(byte[] protectedBytes)
    {
        if (protectedBytes.Length < NonceSize + TagSize)
        {
            throw new ArgumentException("Protected payload is too short.", nameof(protectedBytes));
        }

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipher = new byte[protectedBytes.Length - NonceSize - TagSize];
        Buffer.BlockCopy(protectedBytes, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(protectedBytes, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(protectedBytes, NonceSize + TagSize, cipher, 0, cipher.Length);

        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
