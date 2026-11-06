using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.RequestPool.Commands.UpdateApiRequest;

/// <summary>Updates a pooled request in place.</summary>
public sealed record UpdateApiRequestCommand(
    long Id,
    int ApiId,
    string Title,
    string? Description,
    string? HeadersJson,
    string? QueryStringJson,
    string? RequestBody
) : IRequest<Result>;
