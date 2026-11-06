using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Queries.GetCompareFullDetails;

/// <summary>All per-request details (identity + result + raw field diff) of a compare, for the full export.</summary>
public sealed record GetCompareFullDetailsQuery(string ProjectCode, long CompareId)
    : IRequest<Result<IReadOnlyList<CompareFullDetailItem>>>;
