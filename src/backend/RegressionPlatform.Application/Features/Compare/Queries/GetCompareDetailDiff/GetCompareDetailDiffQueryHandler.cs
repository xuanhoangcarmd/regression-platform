using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Queries.GetCompareDetailDiff;

public sealed class GetCompareDetailDiffQueryHandler(ICompareRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<GetCompareDetailDiffQuery, Result<CompareDetailDiffResponse>>
{
    public async Task<Result<CompareDetailDiffResponse>> Handle(
        GetCompareDetailDiffQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<CompareDetailDiffResponse>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var job = await repository.GetJobAsync(request.CompareId, cancellationToken);
        if (job is null || job.ProjectId != projectId.Value)
        {
            return Result<CompareDetailDiffResponse>.NotFound(
                new Error("Compare.NotFound", $"Compare {request.CompareId} does not exist in the project.")
            );
        }

        if (job.CompareResultId is not long resultId)
        {
            return Result<CompareDetailDiffResponse>.NotFound(
                new Error("Compare.NotReady", $"Compare {request.CompareId} has not finished yet.")
            );
        }

        var diff = await repository.GetDetailDiffAsync(
            job.ProjectId, resultId, job.RunAId, job.RunBId, request.RequestId, job.IgnoreOrder, cancellationToken);
        if (diff is null)
        {
            return Result<CompareDetailDiffResponse>.NotFound(
                new Error("CompareDetail.NotFound", $"Request {request.RequestId} has no compare detail in compare {request.CompareId}.")
            );
        }

        return Result<CompareDetailDiffResponse>.Success(diff);
    }
}
