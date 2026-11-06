namespace RegressionPlatform.Application.Features.Compare.Contracts;

/// <summary>A single request's field-level diff (the raw <c>CompareDetail.DiffJson</c>) with its owning API.</summary>
public sealed record CompareRequestDiff(long RequestId, int ApiId, string? ApiName, string? Method, string? Endpoint, string DiffJson);

/// <summary>
/// Compact, distinct summary of what changed between Run A and Run B — designed as a small,
/// context-rich payload to hand to an AI (or render for review). Change patterns are grouped
/// per API so multiple APIs in one compare stay distinguishable.
/// </summary>
public sealed record CompareChangeSummaryResponse(
    long CompareId,
    string RunA,
    string RunB,
    CompareChangeTotals Totals,
    IReadOnlyList<CompareApiChangeGroup> Apis
);

/// <summary>Distinct change patterns for one API (so multi-API compares aren't lumped together).</summary>
public sealed record CompareApiChangeGroup(
    int ApiId,
    string? ApiName,
    string? Method,
    string? Endpoint,
    int RequestCount,
    int PatternCount,
    IReadOnlyList<CompareChangePattern> Patterns
);

/// <summary>Headline counts for the compare.</summary>
public sealed record CompareChangeTotals(int Requests, int Unchanged, int Changed, int Added, int Removed, int Failed);

/// <summary>
/// One distinct change pattern = (changeType, array-index-normalized path) aggregated across requests,
/// with occurrence/request counts and a few representative old→new samples.
/// </summary>
public sealed record CompareChangePattern(
    string ChangeType,
    string Path,
    int Occurrences,
    int RequestCount,
    int DistinctTransitions,
    IReadOnlyList<CompareChangeSample> Samples
);

/// <summary>An old→new value transition (values rendered as compact, truncated text).</summary>
public sealed record CompareChangeSample(string? Old, string? New);
