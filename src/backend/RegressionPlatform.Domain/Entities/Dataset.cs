namespace RegressionPlatform.Domain.Entities;

/// <summary>
/// A named collection of test requests, scoped to a project and spanning many APIs
/// (e.g. "Mobile Regression"). Each <see cref="ApiRequest"/> belongs to exactly one dataset.
/// Mirrors the <c>Dataset</c> table.
/// </summary>
public class Dataset
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
