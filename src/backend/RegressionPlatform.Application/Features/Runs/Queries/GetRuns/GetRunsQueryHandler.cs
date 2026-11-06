using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Queries.GetRuns;

public sealed class GetRunsQueryHandler(
    IRunRepository repository,
    IProjectRepository projectRepository,
    IRunProgressTracker progressTracker
) : IRequestHandler<GetRunsQuery, Result<IReadOnlyList<RunResponse>>>
{
    private const string RunningStatus = "Running";

    public async Task<Result<IReadOnlyList<RunResponse>>> Handle(GetRunsQuery request, CancellationToken cancellationToken)
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<IReadOnlyList<RunResponse>>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var runs = await repository.GetByProjectAsync(projectId.Value, cancellationToken);

        // Attach live progress (from in-memory tracker) to running runs.
        var enriched = runs.Select(run =>
            {
                if (run.Status != RunningStatus)
                {
                    return run;
                }
                var progress = progressTracker.Get(run.Id);
                // During execution the total lives in the tracker (DB TotalRequest is set only on completion),
                // so surface executed/total and the percentage from memory.
                return progress is { Total: > 0 } p
                    ? run with
                    {
                        ExecutedRequest = p.Executed,
                        TotalRequest = p.Total,
                        ProgressPercent = (int)Math.Round((double)p.Executed / p.Total * 100),
                    }
                    : run;
            })
            .ToList();

        return Result<IReadOnlyList<RunResponse>>.Success(enriched);
    }
}
