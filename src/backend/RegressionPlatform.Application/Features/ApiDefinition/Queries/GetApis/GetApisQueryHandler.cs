using MediatR;
using RegressionPlatform.Application.Features.ApiDefinition.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Pagination;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.ApiDefinition.Queries.GetApis;

public sealed class GetApisQueryHandler(IApiRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<GetApisQuery, Result<PagedResult<ApiResponse>>>
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<PagedResult<ApiResponse>>> Handle(GetApisQuery request, CancellationToken cancellationToken)
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<PagedResult<ApiResponse>>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);

        var parameters = new ApiListParameters(
            projectId.Value,
            request.Search,
            request.IsActive,
            request.SortBy,
            request.SortDirection,
            pageNumber,
            pageSize
        );

        var page = await repository.GetPagedAsync(parameters, cancellationToken);
        return Result<PagedResult<ApiResponse>>.Success(page);
    }
}
