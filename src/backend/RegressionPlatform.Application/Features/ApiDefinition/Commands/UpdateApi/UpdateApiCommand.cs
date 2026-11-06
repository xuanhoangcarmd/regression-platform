using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.ApiDefinition.Commands.UpdateApi;

/// <summary>Updates an existing API definition (within its project).</summary>
public sealed record UpdateApiCommand(
    string ProjectCode,
    int Id,
    string Name,
    string Method,
    string Endpoint,
    string? Description,
    bool IsActive
) : IRequest<Result>;
