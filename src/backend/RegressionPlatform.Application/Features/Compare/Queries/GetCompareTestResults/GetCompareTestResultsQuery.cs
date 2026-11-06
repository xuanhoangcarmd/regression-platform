using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Queries.GetCompareTestResults;

/// <summary>
/// Every request of a compare with its input, BOTH runs' stored responses and the compare outcome —
/// the data behind the report's "Test Results" (Pass/Fail) export.
/// </summary>
public sealed record GetCompareTestResultsQuery(string ProjectCode, long CompareId)
    : IRequest<Result<IReadOnlyList<CompareTestResultItem>>>;
