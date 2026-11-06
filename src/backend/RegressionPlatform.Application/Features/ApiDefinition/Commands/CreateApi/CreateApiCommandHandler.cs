using MediatR;
using RegressionPlatform.Application.Features.ApiDefinition.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Domain.Entities;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.ApiDefinition.Commands.CreateApi;

public sealed class CreateApiCommandHandler(IApiRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<CreateApiCommand, Result<ApiResponse>>
{
    public async Task<Result<ApiResponse>> Handle(CreateApiCommand request, CancellationToken cancellationToken)
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<ApiResponse>.NotFound(new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist."));
        }

        if (await repository.ExistsByNameAsync(projectId.Value, request.Name, excludeId: null, cancellationToken))
        {
            return Result<ApiResponse>.Conflict(new Error("Api.NameExists", $"An API named '{request.Name}' already exists."));
        }

        var api = new Api
        {
            ProjectId = projectId.Value,
            Name = request.Name,
            Method = request.Method,
            Endpoint = request.Endpoint,
            Description = request.Description,
            IsActive = request.IsActive,
        };

        var id = await repository.CreateAsync(api, cancellationToken);

        return Result<ApiResponse>.Success(
            new ApiResponse(id, api.Name, api.Method, api.Endpoint, api.Description, api.IsActive, RequestCount: 0)
        );
    }
}
