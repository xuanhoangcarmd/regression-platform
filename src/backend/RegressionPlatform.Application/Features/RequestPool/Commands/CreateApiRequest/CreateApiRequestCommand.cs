using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.RequestPool.Commands.CreateApiRequest;

/// <summary>Creates a new request in the pool (version 1).</summary>
public sealed record CreateApiRequestCommand(
    int DatasetId,
    int ApiId,
    string Title,
    string? Description,
    string? HeadersJson,
    string? QueryStringJson,
    string? RequestBody
) : IRequest<Result<long>>;
