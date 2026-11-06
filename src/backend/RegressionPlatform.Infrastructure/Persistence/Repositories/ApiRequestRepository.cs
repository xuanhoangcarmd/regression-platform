using Dapper;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.RequestPool;
using RegressionPlatform.Application.Features.RequestPool.Contracts;
using RegressionPlatform.Domain.Entities;
using RegressionPlatform.Infrastructure.Persistence.Sql;
using RegressionPlatform.Shared.Pagination;

namespace RegressionPlatform.Infrastructure.Persistence.Repositories;

/// <summary>Dapper data access for the Request Pool.</summary>
public sealed class ApiRequestRepository(IDbConnectionFactory connectionFactory) : IApiRequestRepository
{
    // Whitelist mapping query sort keys -> SQL columns (guards ORDER BY against injection).
    private static readonly IReadOnlyDictionary<string, string> SortColumns = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["id"] = "r.Id",
        ["apiName"] = "a.Name",
        ["title"] = "r.Title",
        ["description"] = "r.Description",
        ["method"] = "a.Method",
        ["isActive"] = "r.IsActive",
    };

    public async Task<PagedResult<ApiRequestListItemResponse>> GetPagedAsync(
        ApiRequestListParameters parameters,
        CancellationToken cancellationToken
    )
    {
        var orderByColumn = SortColumns.TryGetValue(parameters.SortBy ?? string.Empty, out var column)
            ? column
            : "r.Id";
        var direction = string.Equals(parameters.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";

        var sql = ApiRequestSql.Count + "\n" + ApiRequestSql.GetPaged(orderByColumn, direction);
        var queryParams = new
        {
            parameters.DatasetId,
            parameters.ApiId,
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
        var items = (await multi.ReadAsync<ApiRequestListItemResponse>()).ToList();

        return new PagedResult<ApiRequestListItemResponse>(items, parameters.PageNumber, parameters.PageSize, totalCount);
    }

    public async Task<ApiRequestDetailResponse?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ApiRequestDetailResponse>(
            new CommandDefinition(ApiRequestSql.GetById, new { Id = id }, cancellationToken: cancellationToken)
        );
    }

    public async Task<long> CreateAsync(ApiRequest request, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                ApiRequestSql.Insert,
                new
                {
                    request.DatasetId,
                    request.ApiId,
                    request.Title,
                    request.Description,
                    request.HeadersJson,
                    request.QueryStringJson,
                    request.RequestBody,
                    request.Version,
                    request.IsActive,
                },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task<int> CreateManyAsync(IReadOnlyList<ApiRequest> requests, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        // Dapper executes the insert once per item in the list.
        return await connection.ExecuteAsync(
            new CommandDefinition(
                ApiRequestSql.InsertMany,
                requests.Select(request => new
                {
                    request.DatasetId,
                    request.ApiId,
                    request.Title,
                    request.Description,
                    request.HeadersJson,
                    request.QueryStringJson,
                    request.RequestBody,
                    request.Version,
                    request.IsActive,
                }),
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task<bool> UpdateAsync(ApiRequest request, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                ApiRequestSql.Update,
                new
                {
                    request.Id,
                    request.ApiId,
                    request.Title,
                    request.Description,
                    request.HeadersJson,
                    request.QueryStringJson,
                    request.RequestBody,
                },
                cancellationToken: cancellationToken
            )
        );
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(ApiRequestSql.Delete, new { Id = id }, cancellationToken: cancellationToken)
        );
        return rows > 0;
    }

    public async Task<int> DeleteByApiAsync(int datasetId, int apiId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        var args = new { DatasetId = datasetId, ApiId = apiId };

        // Requests that were run → soft-delete (keep row + responses for history); never-run → hard-delete.
        var softDeleted = await connection.ExecuteAsync(
            new CommandDefinition(ApiRequestSql.SoftDeleteRunByApi, args, transaction, cancellationToken: cancellationToken)
        );
        var hardDeleted = await connection.ExecuteAsync(
            new CommandDefinition(ApiRequestSql.HardDeleteUnrunByApi, args, transaction, cancellationToken: cancellationToken)
        );

        transaction.Commit();
        return softDeleted + hardDeleted;
    }
}
