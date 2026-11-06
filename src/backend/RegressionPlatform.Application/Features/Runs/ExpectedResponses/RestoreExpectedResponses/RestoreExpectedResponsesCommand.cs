using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.ExpectedResponses.RestoreExpectedResponses;

/// <summary>Deletes the edited versions of the given requests (compares use the original again). Returns how many were removed.</summary>
public sealed record RestoreExpectedResponsesCommand(string ProjectCode, long RunId, IReadOnlyList<long> RequestIds)
    : IRequest<Result<int>>;
