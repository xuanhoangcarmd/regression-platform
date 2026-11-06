using MediatR;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Queries.GetRunById;

/// <summary>Gets full detail (Summary) for one run within a project.</summary>
public sealed record GetRunByIdQuery(string ProjectCode, long RunId) : IRequest<Result<RunDetailResponse>>;
