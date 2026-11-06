namespace RegressionPlatform.Domain.Entities;

/// <summary>Per-request compare outcome + field-level diff. Mirrors the <c>CompareDetail</c> table.</summary>
public class CompareDetail
{
    public long Id { get; set; }
    public long CompareResultId { get; set; }
    public long RequestId { get; set; }

    /// <summary>Per-request outcome: Unchanged / Changed / Added / Removed / Failed.</summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>Field-level differences as a JSON array; null when there are none (e.g. Unchanged).</summary>
    public string? DiffJson { get; set; }

    /// <summary>True when an edited (expected) response of either run was used instead of the captured one.</summary>
    public bool IsEdited { get; set; }

    public DateTime CreatedDate { get; set; }
}
