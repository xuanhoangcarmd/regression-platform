using Dapper;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.Datasets;
using RegressionPlatform.Application.Features.Datasets.Contracts;
using RegressionPlatform.Domain.Entities;
using RegressionPlatform.Infrastructure.Persistence.Sql;

namespace RegressionPlatform.Infrastructure.Persistence.Repositories;

/// <summary>Dapper data access for datasets.</summary>
public sealed class DatasetRepository(IDbConnectionFactory connectionFactory) : IDatasetRepository
{
    public async Task<IReadOnlyList<DatasetListItemResponse>> GetByProjectAsync(
        int projectId,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<DatasetListItemResponse>(
            new CommandDefinition(DatasetSql.GetByProject, new { ProjectId = projectId }, cancellationToken: cancellationToken)
        );
        return rows.ToList();
    }

    public async Task<DatasetResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<DatasetResponse>(
            new CommandDefinition(DatasetSql.GetById, new { Id = id }, cancellationToken: cancellationToken)
        );
    }

    public async Task<IReadOnlyList<DatasetApiResponse>> GetApisAsync(int datasetId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<DatasetApiResponse>(
            new CommandDefinition(DatasetSql.GetApis, new { DatasetId = datasetId }, cancellationToken: cancellationToken)
        );
        return rows.ToList();
    }

    public async Task<Dataset?> GetEntityAsync(int id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Dataset>(
            new CommandDefinition(DatasetSql.GetEntityById, new { Id = id }, cancellationToken: cancellationToken)
        );
    }

    public async Task<bool> ExistsByNameAsync(int projectId, string name, int? excludeId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                DatasetSql.ExistsByName,
                new { ProjectId = projectId, Name = name, ExcludeId = excludeId },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task<bool> HasRunsAsync(int id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(DatasetSql.HasRuns, new { Id = id }, cancellationToken: cancellationToken)
        );
    }

    public async Task<int> CreateAsync(Dataset dataset, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                DatasetSql.Insert,
                new
                {
                    dataset.ProjectId,
                    dataset.Name,
                    dataset.Description,
                    dataset.IsActive,
                },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task<bool> UpdateAsync(Dataset dataset, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                DatasetSql.Update,
                new
                {
                    dataset.Id,
                    dataset.Name,
                    dataset.Description,
                    dataset.IsActive,
                },
                cancellationToken: cancellationToken
            )
        );
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(DatasetSql.Delete, new { Id = id }, cancellationToken: cancellationToken)
        );
        return rows > 0;
    }
}
