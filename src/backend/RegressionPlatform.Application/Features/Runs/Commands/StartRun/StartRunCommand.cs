using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Commands.StartRun;

/// <summary>Starts a pending run: moves it to Running and queues it for background execution.</summary>
public sealed record StartRunCommand(string ProjectCode, long RunId) : IRequest<Result>;
