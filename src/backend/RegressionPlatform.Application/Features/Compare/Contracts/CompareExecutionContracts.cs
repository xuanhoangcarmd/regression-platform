namespace RegressionPlatform.Application.Features.Compare.Contracts;

/// <summary>Minimal job info the compare engine needs to execute.</summary>
public sealed record CompareExecutionInfo(long Id, int ProjectId, long RunAId, long RunBId, string BaselineMode, bool IgnoreOrder);

/// <summary>
/// Lightweight per-request response info used for matching + classification (no body loaded;
/// the body is fetched on demand only for requests that need a deep compare). ApiId drives which
/// (API-scoped) ignore rules apply when the bodies are deep-compared.
/// </summary>
public sealed record CompareResponseSummary(
    long RequestId,
    int ApiId,
    /// <summary>Key-sorted canonical hash (order-insensitive identity).</summary>
    string? ResponseHash,
    /// <summary>Original-order hash (strict-order identity); equals ResponseHash for pre-2026-10-19 rows.</summary>
    string? OrderedHash,
    int? StatusCode,
    string? ErrorMessage,
    /// <summary>True when the hash/status/body come from the run's edited (expected) response.</summary>
    bool IsEdited
);
