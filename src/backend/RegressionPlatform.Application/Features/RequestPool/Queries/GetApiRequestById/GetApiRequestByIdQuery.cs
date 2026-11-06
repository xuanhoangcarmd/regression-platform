using MediatR;
using RegressionPlatform.Application.Features.RequestPool.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.RequestPool.Queries.GetApiRequestById;

/// <summary>Gets a single pooled request by id (full detail for editing).</summary>
public sealed record GetApiRequestByIdQuery(long Id) : IRequest<Result<ApiRequestDetailResponse>>;
