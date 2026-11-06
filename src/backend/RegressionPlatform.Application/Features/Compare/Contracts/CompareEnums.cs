namespace RegressionPlatform.Application.Features.Compare.Contracts;

/// <summary>Per-request compare outcome stored in <c>CompareDetail.Result</c>.</summary>
public static class CompareResultTypes
{
    public const string Unchanged = "Unchanged";
    public const string Changed = "Changed";
    public const string Added = "Added";
    public const string Removed = "Removed";
    public const string Failed = "Failed";
}

/// <summary>Which body a compare uses for Run A (<c>CompareJob.BaselineMode</c>).</summary>
public static class CompareBaselineModes
{
    /// <summary>The captured responses only (the original behaviour).</summary>
    public const string Original = "Original";

    /// <summary>Each run's edited (expected) response when one exists, else the captured one.</summary>
    public const string Expected = "Expected";

    public static bool IsValid(string? mode) => mode is Original or Expected;
}

/// <summary>Lifecycle status of a <c>CompareJob</c>.</summary>
public static class CompareStatuses
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}
