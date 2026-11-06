namespace RegressionPlatform.Infrastructure.Persistence.Sql;

/// <summary>Parameterized SQL for the Request Pool (ApiRequest joined to Api).</summary>
internal static class ApiRequestSql
{
    private const string From =
        "FROM ApiRequest r INNER JOIN Api a ON a.Id = r.ApiId "
        + "WHERE r.DatasetId = @DatasetId "
        + "AND r.IsDeleted = 0 "
        + "AND (@ApiId IS NULL OR r.ApiId = @ApiId) "
        + "AND (@Search IS NULL OR r.Title LIKE @Search ESCAPE '\\' OR r.Description LIKE @Search ESCAPE '\\') "
        + "AND (@IsActive IS NULL OR r.IsActive = @IsActive)";

    private const string SelectColumns = "r.Id, r.ApiId, a.Name AS ApiName, r.Title, r.Description, a.Method, r.IsActive";

    public const string Count = $"SELECT COUNT(1) {From};";

    /// <summary><paramref name="orderByColumn"/>/<paramref name="direction"/> MUST come from a whitelist.</summary>
    public static string GetPaged(string orderByColumn, string direction) =>
        $"SELECT {SelectColumns} {From} "
        + $"ORDER BY {orderByColumn} {direction} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

    public const string GetById =
        "SELECT r.Id, r.ApiId, r.Title, r.Description, r.HeadersJson, r.QueryStringJson, r.RequestBody, r.Version, r.IsActive "
        + "FROM ApiRequest r WHERE r.Id = @Id;";

    public const string Insert =
        "INSERT INTO ApiRequest (DatasetId, ApiId, Title, Description, HeadersJson, QueryStringJson, RequestBody, Version, IsActive, CreatedDate) "
        + "OUTPUT INSERTED.Id "
        + "VALUES (@DatasetId, @ApiId, @Title, @Description, @HeadersJson, @QueryStringJson, @RequestBody, @Version, @IsActive, SYSUTCDATETIME());";

    public const string InsertMany =
        "INSERT INTO ApiRequest (DatasetId, ApiId, Title, Description, HeadersJson, QueryStringJson, RequestBody, Version, IsActive, CreatedDate) "
        + "VALUES (@DatasetId, @ApiId, @Title, @Description, @HeadersJson, @QueryStringJson, @RequestBody, @Version, @IsActive, SYSUTCDATETIME());";

    public const string Update =
        "UPDATE ApiRequest SET ApiId = @ApiId, Title = @Title, Description = @Description, HeadersJson = @HeadersJson, "
        + "QueryStringJson = @QueryStringJson, RequestBody = @RequestBody WHERE Id = @Id;";

    public const string Delete = "DELETE FROM ApiRequest WHERE Id = @Id;";

    /// <summary>
    /// Soft-deletes (marks IsDeleted) an API's requests IN a dataset that HAVE captured responses, so
    /// their run/compare history is preserved. Returns rows affected.
    /// </summary>
    public const string SoftDeleteRunByApi =
        "UPDATE ApiRequest SET IsDeleted = 1 "
        + "WHERE DatasetId = @DatasetId AND ApiId = @ApiId AND IsDeleted = 0 "
        + "AND EXISTS (SELECT 1 FROM RunResponse rr WHERE rr.RequestId = ApiRequest.Id);";

    /// <summary>Hard-deletes an API's requests IN a dataset that were never run (no responses). Returns rows affected.</summary>
    public const string HardDeleteUnrunByApi =
        "DELETE FROM ApiRequest "
        + "WHERE DatasetId = @DatasetId AND ApiId = @ApiId "
        + "AND NOT EXISTS (SELECT 1 FROM RunResponse rr WHERE rr.RequestId = ApiRequest.Id);";
}
