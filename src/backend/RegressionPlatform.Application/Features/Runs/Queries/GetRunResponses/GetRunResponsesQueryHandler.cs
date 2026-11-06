using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Pagination;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Queries.GetRunResponses;

public sealed class GetRunResponsesQueryHandler(IRunRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<GetRunResponsesQuery, Result<PagedResult<RunResponseListItem>>>
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<PagedResult<RunResponseListItem>>> Handle(
        GetRunResponsesQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<PagedResult<RunResponseListItem>>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var run = await repository.GetEntityAsync(request.RunId, cancellationToken);
        if (run is null || run.ProjectId != projectId.Value)
        {
            return Result<PagedResult<RunResponseListItem>>.NotFound(
                new Error("Run.NotFound", $"Run {request.RunId} does not exist in the project.")
            );
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var status = string.IsNullOrWhiteSpace(request.Status) || request.Status == "all" ? null : request.Status;

        var parameters = new RunResponseListParameters(
            request.RunId,
            request.Search,
            status,
            pageNumber,
            pageSize,
            request.SortBy,
            request.SortDirection,
            request.ApiId
        );
        var page = await repository.GetResponsesPagedAsync(parameters, cancellationToken);
        return Result<PagedResult<RunResponseListItem>>.Success(page);
    }
}
