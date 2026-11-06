using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Commands.RerunRequest;

/// <summary>Re-runs a single request inside a run and updates its stored result.</summary>
public sealed record RerunRequestCommand(string ProjectCode, long RunId, long RequestId) : IRequest<Result>;
