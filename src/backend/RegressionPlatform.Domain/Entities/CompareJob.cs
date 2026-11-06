namespace RegressionPlatform.Domain.Entities;

/// <summary>An async comparison of two runs. Mirrors the <c>CompareJob</c> table.</summary>
public class CompareJob
{
    public long Id { get; set; }
    public int ProjectId { get; set; }
    public long RunAId { get; set; }
    public long RunBId { get; set; }
    public string Status { get; set; } = string.Empty;

    /// <summary>Live progress 0–100 while Running.</summary>
    public int Progress { get; set; }

    /// <summary>Set when the job finishes successfully (FK → CompareResult).</summary>
    public long? CompareResultId { get; set; }

    /// <summary>Body source: 'Original' (captured) or 'Expected' (each run's edited version when one exists).</summary>
    public string BaselineMode { get; set; } = "Original";

    /// <summary>True (default) = order-insensitive compare; false = object-key and array order are significant.</summary>
    public bool IgnoreOrder { get; set; } = true;

    public DateTime? StartedDate { get; set; }
    public DateTime? FinishedDate { get; set; }
    public DateTime CreatedDate { get; set; }
}
