using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Queries.GetCompareDetailDiff;

/// <summary>Full diff for one request in a compare (field diff + both runs' bodies) for the viewer.</summary>
public sealed record GetCompareDetailDiffQuery(string ProjectCode, long CompareId, long RequestId)
    : IRequest<Result<CompareDetailDiffResponse>>;
