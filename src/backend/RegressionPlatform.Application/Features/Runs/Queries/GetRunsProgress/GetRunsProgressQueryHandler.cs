using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Queries.GetRunsProgress;

public sealed class GetRunsProgressQueryHandler(
    IRunRepository repository,
    IProjectRepository projectRepository,
    IRunProgressTracker progressTracker
) : IRequestHandler<GetRunsProgressQuery, Result<IReadOnlyList<RunProgressResponse>>>
{
    private const string RunningStatus = "Running";

    public async Task<Result<IReadOnlyList<RunProgressResponse>>> Handle(
        GetRunsProgressQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<IReadOnlyList<RunProgressResponse>>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        if (request.RunIds.Count == 0)
        {
            return Result<IReadOnlyList<RunProgressResponse>>.Success([]);
        }

        var rows = await repository.GetProgressByIdsAsync(projectId.Value, request.RunIds, cancellationToken);

        // Overlay live progress (from the in-memory tracker) onto runs still executing. Finished runs
        // are returned as-is, so the caller sees the new Status + final counts and can stop polling them.
        var enriched = rows.Select(row =>
            {
                if (row.Status != RunningStatus)
                {
                    return row;
                }
                var progress = progressTracker.Get(row.Id);
                return progress is { Total: > 0 } p
                    ? row with
                    {
                        ExecutedRequest = p.Executed,
                        TotalRequest = p.Total,
                        ProgressPercent = (int)Math.Round((double)p.Executed / p.Total * 100),
                    }
                    : row;
            })
            .ToList();

        return Result<IReadOnlyList<RunProgressResponse>>.Success(enriched);
    }
}
