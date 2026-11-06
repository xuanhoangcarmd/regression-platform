using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Queries.GetCompareTestResults;

public sealed class GetCompareTestResultsQueryHandler(ICompareRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<GetCompareTestResultsQuery, Result<IReadOnlyList<CompareTestResultItem>>>
{
    public async Task<Result<IReadOnlyList<CompareTestResultItem>>> Handle(
        GetCompareTestResultsQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<IReadOnlyList<CompareTestResultItem>>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var job = await repository.GetJobAsync(request.CompareId, cancellationToken);
        if (job is null || job.ProjectId != projectId.Value)
        {
            return Result<IReadOnlyList<CompareTestResultItem>>.NotFound(
                new Error("Compare.NotFound", $"Compare {request.CompareId} does not exist in the project.")
            );
        }

        if (job.CompareResultId is not long resultId)
        {
            return Result<IReadOnlyList<CompareTestResultItem>>.Success([]); // not finished yet
        }

        var rows = await repository.GetTestResultsAsync(resultId, job.RunAId, job.RunBId, cancellationToken);
        return Result<IReadOnlyList<CompareTestResultItem>>.Success(rows);
    }
}
