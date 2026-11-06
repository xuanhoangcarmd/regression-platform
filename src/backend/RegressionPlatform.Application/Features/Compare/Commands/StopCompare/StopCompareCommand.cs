using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Commands.StopCompare;

/// <summary>Stops a running compare: marks it Cancelled and signals the background worker to abort it.</summary>
public sealed record StopCompareCommand(string ProjectCode, long CompareId) : IRequest<Result>;
