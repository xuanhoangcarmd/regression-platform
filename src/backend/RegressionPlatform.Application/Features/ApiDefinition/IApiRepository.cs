using RegressionPlatform.Application.Features.ApiDefinition.Contracts;
using RegressionPlatform.Domain.Entities;
using RegressionPlatform.Shared.Pagination;

namespace RegressionPlatform.Application.Features.ApiDefinition;

/// <summary>Data access for <see cref="Api"/> definitions. Implemented in Infrastructure.</summary>
public interface IApiRepository
{
    Task<PagedResult<ApiResponse>> GetPagedAsync(ApiListParameters parameters, CancellationToken cancellationToken);

    Task<ApiResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(Api api, CancellationToken cancellationToken);

    /// <summary>Bulk-inserts APIs; returns the number of rows inserted.</summary>
    Task<int> CreateManyAsync(IReadOnlyList<Api> apis, CancellationToken cancellationToken);

    /// <summary>Existing "METHOD|Endpoint" keys within a project, used to skip duplicates on import.</summary>
    Task<IReadOnlyList<string>> GetMethodEndpointKeysAsync(int projectId, CancellationToken cancellationToken);

    /// <summary>Existing API names within a project (Name is unique per project), used to avoid collisions.</summary>
    Task<IReadOnlyList<string>> GetNamesAsync(int projectId, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(Api api, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>True if another API in the project already uses <paramref name="name"/> (Name is unique per project).</summary>
    Task<bool> ExistsByNameAsync(int projectId, string name, int? excludeId, CancellationToken cancellationToken);
}
