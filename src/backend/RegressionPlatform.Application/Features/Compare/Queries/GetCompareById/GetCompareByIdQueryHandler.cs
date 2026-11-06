using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Queries.GetCompareById;

public sealed class GetCompareByIdQueryHandler(ICompareRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<GetCompareByIdQuery, Result<CompareSummaryResponse>>
{
    public async Task<Result<CompareSummaryResponse>> Handle(
        GetCompareByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<CompareSummaryResponse>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var summary = await repository.GetSummaryAsync(request.CompareId, cancellationToken);
        if (summary is null || summary.ProjectId != projectId.Value)
        {
            return Result<CompareSummaryResponse>.NotFound(
                new Error("Compare.NotFound", $"Compare {request.CompareId} does not exist in the project.")
            );
        }

        return Result<CompareSummaryResponse>.Success(summary);
    }
}
