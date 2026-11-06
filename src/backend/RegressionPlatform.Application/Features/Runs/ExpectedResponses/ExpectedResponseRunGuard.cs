using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.Runs.ExpectedResponses;

/// <summary>Shared checks for the Expected Response endpoints: the run exists in the project (and is editable).</summary>
internal static class ExpectedResponseRunGuard
{
    /// <summary>Statuses whose responses may still change — editing is blocked while a run executes.</summary>
    private static readonly HashSet<string> ActiveStatuses = new(StringComparer.OrdinalIgnoreCase) { "Pending", "Queued", "Running" };

    /// <summary>The run when it belongs to the project; otherwise a NotFound error.</summary>
    public static async Task<(DomainEntities.Run? Run, Error? Error)> FindAsync(
        IProjectRepository projectRepository,
        IRunRepository runRepository,
        string projectCode,
        long runId,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(projectCode, cancellationToken);
        if (projectId is null)
        {
            return (null, new Error("Project.NotFound", $"Project '{projectCode}' does not exist."));
        }

        var run = await runRepository.GetEntityAsync(runId, cancellationToken);
        return run is null || run.ProjectId != projectId.Value
            ? (null, new Error("Run.NotFound", $"Run {runId} does not exist in the project."))
            : (run, null);
    }

    /// <summary>A Conflict error when the run is still pending / executing, else null.</summary>
    public static Error? EnsureEditable(DomainEntities.Run run) =>
        ActiveStatuses.Contains(run.Status)
            ? new Error("Run.NotFinished", "Expected responses can only be edited after the run has finished.")
            : null;
}
