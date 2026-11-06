namespace RegressionPlatform.Domain.Entities;

/// <summary>A project (config scope). Mirrors the <c>Project</c> table (docs/database/Tables.md).</summary>
public class Project
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
