using MediatR;
using RegressionPlatform.Application.Features.ApiDefinition.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.ApiDefinition.Commands.CreateApi;

/// <summary>Creates a new API definition in a project.</summary>
public sealed record CreateApiCommand(
    string ProjectCode,
    string Name,
    string Method,
    string Endpoint,
    string? Description,
    bool IsActive
) : IRequest<Result<ApiResponse>>;
