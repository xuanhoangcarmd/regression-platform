namespace RegressionPlatform.Domain.Entities;

/// <summary>
/// A request header for a configuration's environment. Mirrors the
/// <c>ProjectConfigurationHeader</c> table (docs/database/Tables.md).
/// Secret values are stored encrypted in <see cref="SecretValueEncrypted"/>, never in
/// <see cref="HeaderValue"/> (plaintext is for non-secret headers only).
/// </summary>
public class ProjectConfigurationHeader
{
    public int Id { get; set; }
    public int ConfigurationId { get; set; }
    public string HeaderKey { get; set; } = string.Empty;
    public string? HeaderValue { get; set; }
    public byte[]? SecretValueEncrypted { get; set; }
    public bool IsSecret { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
