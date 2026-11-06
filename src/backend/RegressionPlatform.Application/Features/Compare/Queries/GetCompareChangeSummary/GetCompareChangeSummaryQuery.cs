using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Queries.GetCompareChangeSummary;

/// <summary>Distinct change patterns of a compare (compact summary for review / AI).</summary>
public sealed record GetCompareChangeSummaryQuery(string ProjectCode, long Id)
    : IRequest<Result<CompareChangeSummaryResponse>>;
