using MediatR;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.ExpectedResponses.GetExpectedRequests;

/// <summary>A run's executed requests of one API (no bodies) with their edited flag.</summary>
public sealed record GetExpectedRequestsQuery(string ProjectCode, long RunId, int ApiId)
    : IRequest<Result<IReadOnlyList<ExpectedRequestItem>>>;
