using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Commands.RerunRun;

/// <summary>
/// Re-runs a finished run (Completed / Failed / Cancelled) from scratch: discards its captured
/// responses, resets its status + counts and queues it for background execution again.
/// </summary>
public sealed record RerunRunCommand(string ProjectCode, long RunId) : IRequest<Result>;
