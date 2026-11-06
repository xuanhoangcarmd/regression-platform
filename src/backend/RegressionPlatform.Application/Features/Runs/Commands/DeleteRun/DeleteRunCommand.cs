using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Commands.DeleteRun;

/// <summary>Deletes a run and ALL data derived from it (responses, API links, compares, reports).</summary>
public sealed record DeleteRunCommand(string ProjectCode, long RunId) : IRequest<Result>;
