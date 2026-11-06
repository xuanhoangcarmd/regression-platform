using MediatR;
using RegressionPlatform.Application.Features.ApiDefinition.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.ApiDefinition.Queries.GetApiById;

/// <summary>Gets a single API definition by id.</summary>
public sealed record GetApiByIdQuery(int Id) : IRequest<Result<ApiResponse>>;
