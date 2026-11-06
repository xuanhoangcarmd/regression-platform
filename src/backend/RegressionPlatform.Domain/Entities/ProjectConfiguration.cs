namespace RegressionPlatform.Domain.Entities;

/// <summary>
/// Per-project, per-environment configuration (Base URL + headers).
/// Mirrors the <c>ProjectConfiguration</c> table (docs/database/Tables.md).
/// <see cref="Headers"/> is populated by the repository (composition), not a DB column.
/// </summary>
public class ProjectConfiguration
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Environment { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public List<ProjectConfigurationHeader> Headers { get; set; } = [];
}
