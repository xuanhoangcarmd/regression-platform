namespace RegressionPlatform.Domain.Entities;

/// <summary>Summary of a Run A vs Run B comparison. Mirrors the <c>CompareResult</c> table.</summary>
public class CompareResult
{
    public long Id { get; set; }
    public long RunAId { get; set; }
    public long RunBId { get; set; }
    public int TotalRequest { get; set; }
    public int UnchangedCount { get; set; }
    public int ChangedCount { get; set; }
    public int FailedCount { get; set; }
    public int AddedCount { get; set; }
    public int RemovedCount { get; set; }

    /// <summary>Reserved; not populated by the current per-request result set.</summary>
    public int ModifiedCount { get; set; }

    public DateTime CreatedDate { get; set; }
}
