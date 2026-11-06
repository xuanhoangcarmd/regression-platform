namespace RegressionPlatform.Domain.Entities;

/// <summary>A regression run (execution). Mirrors the <c>Run</c> table (docs/database/Tables.md).</summary>
public class Run
{
    public long Id { get; set; }
    public int ProjectId { get; set; }
    /// <summary>The dataset whose requests this run executes.</summary>
    public int DatasetId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Environment { get; set; } = string.Empty;
    public string RunType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    /// <summary>Max requests to run per API; null = run all of each API's requests.</summary>
    public int? RequestLimitPerApi { get; set; }
    /// <summary>How many APIs execute concurrently (1 = sequential across APIs).</summary>
    public int MaxParallelApis { get; set; } = 1;
    /// <summary>Within one API, how many requests execute concurrently (1 = sequential).</summary>
    public int MaxParallelRequestsPerApi { get; set; } = 1;
    public int? TotalRequest { get; set; }
    /// <summary>Execution health, set when the run completes (null while pending/running).</summary>
    public int? PassCount { get; set; }
    public int? WarningCount { get; set; }
    public int? FailedCount { get; set; }
    public long? DurationMs { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime CreatedDate { get; set; }
}
