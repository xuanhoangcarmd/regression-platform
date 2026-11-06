using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Commands.StartCompare;

/// <summary>Starts a Pending compare (queues it for background execution).</summary>
public sealed record StartCompareCommand(string ProjectCode, long Id) : IRequest<Result>;
