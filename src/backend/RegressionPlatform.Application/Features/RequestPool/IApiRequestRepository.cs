using RegressionPlatform.Application.Features.RequestPool.Contracts;
using RegressionPlatform.Domain.Entities;
using RegressionPlatform.Shared.Pagination;

namespace RegressionPlatform.Application.Features.RequestPool;

/// <summary>Data access for the Request Pool (ApiRequest). Implemented in Infrastructure.</summary>
public interface IApiRequestRepository
{
    Task<PagedResult<ApiRequestListItemResponse>> GetPagedAsync(
        ApiRequestListParameters parameters,
        CancellationToken cancellationToken
    );

    Task<ApiRequestDetailResponse?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<long> CreateAsync(ApiRequest request, CancellationToken cancellationToken);

    /// <summary>Bulk-inserts requests; returns the number of rows inserted.</summary>
    Task<int> CreateManyAsync(IReadOnlyList<ApiRequest> requests, CancellationToken cancellationToken);

    /// <summary>Updates a request in place. Returns true if a row changed.</summary>
    Task<bool> UpdateAsync(ApiRequest request, CancellationToken cancellationToken);

    /// <summary>Deletes a request. Returns true if a row was removed.</summary>
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes ALL requests of an API within a dataset, plus their captured run responses (one
    /// transaction). Returns how many requests were removed.
    /// </summary>
    Task<int> DeleteByApiAsync(int datasetId, int apiId, CancellationToken cancellationToken);
}
