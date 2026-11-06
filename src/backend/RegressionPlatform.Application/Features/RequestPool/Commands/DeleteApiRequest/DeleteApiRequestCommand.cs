using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.RequestPool.Commands.DeleteApiRequest;

/// <summary>Deletes a request from the pool.</summary>
public sealed record DeleteApiRequestCommand(long Id) : IRequest<Result>;
