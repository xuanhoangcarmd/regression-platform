namespace RegressionPlatform.Domain.Entities;

/// <summary>
/// A point-in-time regression report for a single Completed compare (1 Report = 1 Compare).
/// Headline counts + computed score/risk are snapshotted at generation time. Mirrors the
/// <c>Report</c> table.
/// </summary>
public class Report
{
    public long Id { get; set; }
    public int ProjectId { get; set; }

    /// <summary>The compare this report covers (unique — one report per compare).</summary>
    public long CompareJobId { get; set; }

    /// <summary>The finished result the snapshot was taken from (FK → CompareResult).</summary>
    public long CompareResultId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Lifecycle status (Completed for now; async AI phases may add Pending/Generating).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>0–100: unchanged share of the compare (see docs — tunable formula).</summary>
    public decimal RegressionScore { get; set; }

    /// <summary>Low / Medium / High / Critical, derived from the breaking-API count.</summary>
    public string RiskLevel { get; set; } = string.Empty;

    // Snapshot of the compare's result counts.
    public int TotalRequest { get; set; }
    public int UnchangedCount { get; set; }
    public int ChangedCount { get; set; }
    public int AddedCount { get; set; }
    public int RemovedCount { get; set; }
    public int FailedCount { get; set; }
    public int TotalApis { get; set; }
    public int BreakingApiCount { get; set; }

    public DateTime GeneratedDate { get; set; }
    public DateTime CreatedDate { get; set; }
}
