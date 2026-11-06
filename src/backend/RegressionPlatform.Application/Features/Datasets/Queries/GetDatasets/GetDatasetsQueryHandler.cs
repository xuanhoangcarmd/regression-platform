using MediatR;
using RegressionPlatform.Application.Features.Datasets.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Datasets.Queries.GetDatasets;

public sealed class GetDatasetsQueryHandler(IDatasetRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<GetDatasetsQuery, Result<IReadOnlyList<DatasetListItemResponse>>>
{
    public async Task<Result<IReadOnlyList<DatasetListItemResponse>>> Handle(
        GetDatasetsQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<IReadOnlyList<DatasetListItemResponse>>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var datasets = await repository.GetByProjectAsync(projectId.Value, cancellationToken);
        return Result<IReadOnlyList<DatasetListItemResponse>>.Success(datasets);
    }
}
