namespace RegressionPlatform.Application.Abstractions;

/// <summary>
/// Encrypts/decrypts sensitive values for storage at rest (e.g. secret configuration headers).
/// Implemented in Infrastructure. Secrets must never be persisted in plaintext
/// (see docs/rules/database/security.md).
/// </summary>
public interface ISecretProtector
{
    /// <summary>Encrypts plaintext into an opaque blob (safe to store as varbinary).</summary>
    byte[] Encrypt(string plaintext);

    /// <summary>Decrypts a blob previously produced by <see cref="Encrypt"/>.</summary>
    string Decrypt(byte[] protectedBytes);
}
