using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Queries.GetRunForEdit;

public sealed class GetRunForEditQueryHandler(
    IRunRepository runRepository,
    IProjectRepository projectRepository
) : IRequestHandler<GetRunForEditQuery, Result<RunEditResponse>>
{
    public async Task<Result<RunEditResponse>> Handle(GetRunForEditQuery request, CancellationToken cancellationToken)
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<RunEditResponse>.NotFound(new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist."));
        }

        var run = await runRepository.GetEntityAsync(request.RunId, cancellationToken);
        if (run is null || run.ProjectId != projectId.Value)
        {
            return Result<RunEditResponse>.NotFound(new Error("Run.NotFound", $"Run {request.RunId} does not exist in the project."));
        }

        var apiIds = await runRepository.GetApiIdsAsync(request.RunId, cancellationToken);
        return Result<RunEditResponse>.Success(new RunEditResponse(
            run.Id, run.DatasetId, run.Title, run.Description, run.Environment, run.RunType,
            run.RequestLimitPerApi, run.MaxParallelApis, run.MaxParallelRequestsPerApi, apiIds
        ));
    }
}
