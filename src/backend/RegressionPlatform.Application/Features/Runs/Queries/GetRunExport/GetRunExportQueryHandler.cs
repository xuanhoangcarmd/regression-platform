using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Queries.GetRunExport;

public sealed class GetRunExportQueryHandler(IRunRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<GetRunExportQuery, Result<IReadOnlyList<RunResponseExportRow>>>
{
    public async Task<Result<IReadOnlyList<RunResponseExportRow>>> Handle(
        GetRunExportQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<IReadOnlyList<RunResponseExportRow>>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var run = await repository.GetEntityAsync(request.RunId, cancellationToken);
        if (run is null || run.ProjectId != projectId.Value)
        {
            return Result<IReadOnlyList<RunResponseExportRow>>.NotFound(
                new Error("Run.NotFound", $"Run {request.RunId} does not exist in the project.")
            );
        }

        var rows = await repository.GetResponsesForExportAsync(request.RunId, cancellationToken);
        return Result<IReadOnlyList<RunResponseExportRow>>.Success(rows);
    }
}
