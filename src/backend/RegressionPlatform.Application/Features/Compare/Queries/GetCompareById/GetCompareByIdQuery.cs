using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Queries.GetCompareById;

/// <summary>Detail header (job + result summary) for a compare.</summary>
public sealed record GetCompareByIdQuery(string ProjectCode, long CompareId)
    : IRequest<Result<CompareSummaryResponse>>;
