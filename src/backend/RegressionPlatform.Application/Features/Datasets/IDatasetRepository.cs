using RegressionPlatform.Application.Features.Datasets.Contracts;
using RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.Datasets;

/// <summary>Data access for datasets (named request collections). Implemented in Infrastructure.</summary>
public interface IDatasetRepository
{
    /// <summary>All datasets in a project (with request + API counts), ordered by name.</summary>
    Task<IReadOnlyList<DatasetListItemResponse>> GetByProjectAsync(int projectId, CancellationToken cancellationToken);

    /// <summary>A single dataset for the detail header / edit prefill, or null if not found.</summary>
    Task<DatasetResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Distinct APIs that have requests in the dataset (with per-API request counts).</summary>
    Task<IReadOnlyList<DatasetApiResponse>> GetApisAsync(int datasetId, CancellationToken cancellationToken);

    /// <summary>The dataset entity (for ownership/status validation), or null if not found.</summary>
    Task<Dataset?> GetEntityAsync(int id, CancellationToken cancellationToken);

    /// <summary>True if another dataset in the project already uses <paramref name="name"/> (unique per project).</summary>
    Task<bool> ExistsByNameAsync(int projectId, string name, int? excludeId, CancellationToken cancellationToken);

    /// <summary>
    /// True if any run still references the dataset (blocks deletion — run history would break).
    /// The dataset's own requests do not block: <see cref="DeleteAsync"/> removes them with it.
    /// </summary>
    Task<bool> HasRunsAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(Dataset dataset, CancellationToken cancellationToken);

    /// <summary>Updates name/description/active state. Returns true if a row changed.</summary>
    Task<bool> UpdateAsync(Dataset dataset, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a dataset together with its requests. Only valid for a dataset with no runs
    /// (callers must check <see cref="HasRunsAsync"/> first). Returns true if a row was removed.
    /// </summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
