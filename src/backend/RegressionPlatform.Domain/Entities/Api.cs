namespace RegressionPlatform.Domain.Entities;

/// <summary>An API endpoint definition. Mirrors the <c>Api</c> table (docs/database/Tables.md).</summary>
public class Api
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
