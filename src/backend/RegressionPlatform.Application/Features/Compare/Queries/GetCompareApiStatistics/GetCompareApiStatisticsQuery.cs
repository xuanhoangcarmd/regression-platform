using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Queries.GetCompareApiStatistics;

/// <summary>Per-API breakdown ("Change by API") for a compare.</summary>
public sealed record GetCompareApiStatisticsQuery(string ProjectCode, long CompareId)
    : IRequest<Result<IReadOnlyList<CompareApiStatistic>>>;
