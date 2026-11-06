using MediatR;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Queries.GetRunResponseDetail;

/// <summary>Full detail of one executed request in a run (Results tab): body, headers, and the sent request.</summary>
public sealed record GetRunResponseDetailQuery(string ProjectCode, long RunId, long RequestId)
    : IRequest<Result<RunResponseDetail>>;
