using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Queries.GetRunById;

public sealed class GetRunByIdQueryHandler(IRunRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<GetRunByIdQuery, Result<RunDetailResponse>>
{
    public async Task<Result<RunDetailResponse>> Handle(GetRunByIdQuery request, CancellationToken cancellationToken)
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<RunDetailResponse>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var run = await repository.GetEntityAsync(request.RunId, cancellationToken);
        if (run is null || run.ProjectId != projectId.Value)
        {
            return Result<RunDetailResponse>.NotFound(
                new Error("Run.NotFound", $"Run {request.RunId} does not exist in the project.")
            );
        }

        var statistics = await repository.GetApiStatisticsAsync(request.RunId, cancellationToken);

        var detail = new RunDetailResponse(
            run.Id,
            run.Title,
            run.Description,
            run.Environment,
            run.RunType,
            run.Status,
            run.TotalRequest,
            run.PassCount,
            run.WarningCount,
            run.FailedCount,
            run.StartTime,
            run.EndTime,
            run.DurationMs,
            run.CreatedDate,
            statistics.Count,
            statistics
        );

        return Result<RunDetailResponse>.Success(detail);
    }
}
