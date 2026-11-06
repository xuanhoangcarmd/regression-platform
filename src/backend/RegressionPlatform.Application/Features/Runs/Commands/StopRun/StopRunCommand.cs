using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Commands.StopRun;

/// <summary>Stops a running run: marks it Cancelled and signals the background worker to abort it.</summary>
public sealed record StopRunCommand(string ProjectCode, long RunId) : IRequest<Result>;
