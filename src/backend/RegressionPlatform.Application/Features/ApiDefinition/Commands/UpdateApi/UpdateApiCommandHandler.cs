using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Domain.Entities;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.ApiDefinition.Commands.UpdateApi;

public sealed class UpdateApiCommandHandler(IApiRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<UpdateApiCommand, Result>
{
    public async Task<Result> Handle(UpdateApiCommand request, CancellationToken cancellationToken)
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result.NotFound(new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist."));
        }

        var existing = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            return Result.NotFound(new Error("Api.NotFound", $"API {request.Id} was not found."));
        }

        if (await repository.ExistsByNameAsync(projectId.Value, request.Name, excludeId: request.Id, cancellationToken))
        {
            return Result.Conflict(new Error("Api.NameExists", $"An API named '{request.Name}' already exists."));
        }

        var api = new Api
        {
            Id = request.Id,
            Name = request.Name,
            Method = request.Method,
            Endpoint = request.Endpoint,
            Description = request.Description,
            IsActive = request.IsActive,
        };

        await repository.UpdateAsync(api, cancellationToken);
        return Result.Success();
    }
}
