using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Runs;
using RegressionPlatform.Shared.Results;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.Compare.Commands.CreateCompare;

public sealed class CreateCompareCommandHandler(
    ICompareRepository repository,
    IRunRepository runRepository,
    IProjectRepository projectRepository
) : IRequestHandler<CreateCompareCommand, Result<CompareJobListItem>>
{
    public async Task<Result<CompareJobListItem>> Handle(CreateCompareCommand request, CancellationToken cancellationToken)
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<CompareJobListItem>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var runA = await runRepository.GetEntityAsync(request.RunAId, cancellationToken);
        if (runA is null || runA.ProjectId != projectId.Value)
        {
            return Result<CompareJobListItem>.NotFound(
                new Error("Run.NotFound", $"Run {request.RunAId} does not exist in the project.")
            );
        }

        var runB = await runRepository.GetEntityAsync(request.RunBId, cancellationToken);
        if (runB is null || runB.ProjectId != projectId.Value)
        {
            return Result<CompareJobListItem>.NotFound(
                new Error("Run.NotFound", $"Run {request.RunBId} does not exist in the project.")
            );
        }

        if (runA.Status != CompareStatuses.Completed || runB.Status != CompareStatuses.Completed)
        {
            return Result<CompareJobListItem>.Failure(
                new Error("Compare.RunNotCompleted", "Both runs must be Completed to compare.")
            );
        }

        var job = new DomainEntities.CompareJob
        {
            ProjectId = projectId.Value,
            RunAId = request.RunAId,
            RunBId = request.RunBId,
            Status = CompareStatuses.Pending,
            BaselineMode = request.BaselineMode ?? CompareBaselineModes.Original,
            IgnoreOrder = request.IgnoreOrder ?? true,
        };

        // Created in Pending status; the user starts it explicitly (POST /compares/{id}/start).
        var id = await repository.CreateJobAsync(job, request.ApiIds, cancellationToken);

        var summary = await repository.GetSummaryAsync(id, cancellationToken);
        if (summary is null)
        {
            return Result<CompareJobListItem>.Failure(
                new Error("Compare.CreateFailed", "Failed to create the compare.")
            );
        }

        var listItem = new CompareJobListItem(
            summary.Id,
            summary.RunAId,
            summary.RunBId,
            summary.RunATitle,
            summary.RunBTitle,
            summary.Status,
            summary.Progress,
            summary.CompareResultId,
            summary.TotalRequest,
            summary.UnchangedCount,
            summary.ChangedCount,
            summary.AddedCount,
            summary.RemovedCount,
            summary.FailedCount,
            summary.FinishedDate,
            summary.CreatedDate,
            HasReport: false // a just-created compare has no report yet
        );

        return Result<CompareJobListItem>.Success(listItem);
    }
}
