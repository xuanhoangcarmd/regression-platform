using Dapper;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.ApiDefinition;
using RegressionPlatform.Application.Features.ApiDefinition.Contracts;
using RegressionPlatform.Domain.Entities;
using RegressionPlatform.Infrastructure.Persistence.Sql;
using RegressionPlatform.Shared.Pagination;

namespace RegressionPlatform.Infrastructure.Persistence.Repositories;

/// <summary>Dapper data access for <see cref="Api"/> definitions.</summary>
public sealed class ApiRepository(IDbConnectionFactory connectionFactory) : IApiRepository
{
    // Whitelist mapping query sort keys -> SQL columns (guards ORDER BY against injection).
    private static readonly IReadOnlyDictionary<string, string> SortColumns = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["id"] = "a.Id",
        ["name"] = "a.Name",
        ["method"] = "a.Method",
        ["endpoint"] = "a.Endpoint",
        ["requestCount"] = "RequestCount",
        ["isActive"] = "a.IsActive",
    };

    public async Task<PagedResult<ApiResponse>> GetPagedAsync(
        ApiListParameters parameters,
        CancellationToken cancellationToken
    )
    {
        var orderByColumn = SortColumns.TryGetValue(parameters.SortBy ?? string.Empty, out var column)
            ? column
            : "a.Id";
        var direction = string.Equals(parameters.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";

        var sql = ApiSql.Count + "\n" + ApiSql.GetPaged(orderByColumn, direction);
        var queryParams = new
        {
            parameters.ProjectId,
            Search = SqlSearch.Contains(parameters.Search),
            parameters.IsActive,
            Offset = (parameters.PageNumber - 1) * parameters.PageSize,
            parameters.PageSize,
        };

        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, queryParams, cancellationToken: cancellationToken)
        );

        var totalCount = await multi.ReadFirstAsync<int>();
        var items = (await multi.ReadAsync<ApiResponse>()).ToList();

        return new PagedResult<ApiResponse>(items, parameters.PageNumber, parameters.PageSize, totalCount);
    }

    public async Task<ApiResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ApiResponse>(
            new CommandDefinition(ApiSql.GetById, new { Id = id }, cancellationToken: cancellationToken)
        );
    }

    public async Task<int> CreateAsync(Api api, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                ApiSql.Insert,
                new
                {
                    api.ProjectId,
                    api.Name,
                    api.Method,
                    api.Endpoint,
                    api.Description,
                    api.IsActive,
                },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task<int> CreateManyAsync(IReadOnlyList<Api> apis, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        // Dapper executes the insert once per item in the list.
        return await connection.ExecuteAsync(
            new CommandDefinition(ApiSql.InsertMany, apis, cancellationToken: cancellationToken)
        );
    }

    public async Task<IReadOnlyList<string>> GetMethodEndpointKeysAsync(int projectId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var keys = await connection.QueryAsync<string>(
            new CommandDefinition(ApiSql.MethodEndpointKeys, new { ProjectId = projectId }, cancellationToken: cancellationToken)
        );
        return keys.ToList();
    }

    public async Task<IReadOnlyList<string>> GetNamesAsync(int projectId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var names = await connection.QueryAsync<string>(
            new CommandDefinition(ApiSql.Names, new { ProjectId = projectId }, cancellationToken: cancellationToken)
        );
        return names.ToList();
    }

    public async Task<bool> UpdateAsync(Api api, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                ApiSql.Update,
                new
                {
                    api.Id,
                    api.Name,
                    api.Method,
                    api.Endpoint,
                    api.Description,
                    api.IsActive,
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
            new CommandDefinition(ApiSql.Delete, new { Id = id }, cancellationToken: cancellationToken)
        );
        return rows > 0;
    }

    public async Task<bool> ExistsByNameAsync(int projectId, string name, int? excludeId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                ApiSql.ExistsByName,
                new { ProjectId = projectId, Name = name, ExcludeId = excludeId },
                cancellationToken: cancellationToken
            )
        );
    }
}
