using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Commands.CreateCompare;

/// <summary>
/// Creates a compare job for Run A vs Run B (both must be Completed and in the project) in Pending
/// status; the user starts it explicitly. Empty <paramref name="ApiIds"/> = compare all requests.
/// <paramref name="BaselineMode"/>: 'Expected' uses each run's edited responses when available; null /
/// 'Original' = the captured responses only (the original behaviour).
/// </summary>
public sealed record CreateCompareCommand(
    string ProjectCode,
    long RunAId,
    long RunBId,
    IReadOnlyList<int> ApiIds,
    string? BaselineMode = null,
    /// <summary>Null / true = ignore order (default); false = strict order.</summary>
    bool? IgnoreOrder = null
) : IRequest<Result<CompareJobListItem>>;
