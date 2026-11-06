using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Pagination;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Queries.GetCompareDetails;

public sealed class GetCompareDetailsQueryHandler(ICompareRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<GetCompareDetailsQuery, Result<PagedResult<CompareDetailListItem>>>
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<PagedResult<CompareDetailListItem>>> Handle(
        GetCompareDetailsQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<PagedResult<CompareDetailListItem>>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var job = await repository.GetJobAsync(request.CompareId, cancellationToken);
        if (job is null || job.ProjectId != projectId.Value)
        {
            return Result<PagedResult<CompareDetailListItem>>.NotFound(
                new Error("Compare.NotFound", $"Compare {request.CompareId} does not exist in the project.")
            );
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);

        if (job.CompareResultId is not long resultId)
        {
            // Not finished yet — empty page.
            return Result<PagedResult<CompareDetailListItem>>.Success(
                new PagedResult<CompareDetailListItem>([], pageNumber, pageSize, 0)
            );
        }

        var result = string.IsNullOrWhiteSpace(request.Result) || request.Result == "all" ? null : request.Result;
        var parameters = new CompareDetailListParameters(
            resultId,
            request.Search,
            result,
            pageNumber,
            pageSize,
            request.SortBy,
            request.SortDirection,
            request.ApiId,
            string.IsNullOrWhiteSpace(request.ChangeType) ? null : request.ChangeType,
            string.IsNullOrWhiteSpace(request.ChangePath) ? null : request.ChangePath
        );

        var page = await repository.GetDetailsPagedAsync(parameters, cancellationToken);
        return Result<PagedResult<CompareDetailListItem>>.Success(page);
    }
}
