namespace RegressionPlatform.Infrastructure.Persistence.Sql;

/// <summary>Parameterized SQL for the <c>Api</c> table. SQL text lives here, never in handlers (sql.md).</summary>
internal static class ApiSql
{
    private const string SelectColumns =
        "a.Id, a.Name, a.Method, a.Endpoint, a.Description, a.IsActive, "
        + "(SELECT COUNT(1) FROM ApiRequest r WHERE r.ApiId = a.Id AND r.IsDeleted = 0) AS RequestCount";

    private const string WhereFilter =
        "WHERE a.ProjectId = @ProjectId "
        + "AND (@Search IS NULL OR a.Name LIKE @Search ESCAPE '\\' OR a.Endpoint LIKE @Search ESCAPE '\\') "
        + "AND (@IsActive IS NULL OR a.IsActive = @IsActive)";

    public const string GetById = $"SELECT {SelectColumns} FROM Api a WHERE a.Id = @Id;";

    public const string Count = $"SELECT COUNT(1) FROM Api a {WhereFilter};";

    /// <summary>
    /// Paged list. <paramref name="orderByColumn"/> and <paramref name="direction"/> MUST come from a
    /// validated whitelist (never user input) — they are interpolated, not parameterized.
    /// </summary>
    public static string GetPaged(string orderByColumn, string direction) =>
        $"SELECT {SelectColumns} FROM Api a {WhereFilter} "
        + $"ORDER BY {orderByColumn} {direction} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

    public const string ExistsByName =
        "SELECT CASE WHEN EXISTS (SELECT 1 FROM Api WHERE ProjectId = @ProjectId AND Name = @Name "
        + "AND (@ExcludeId IS NULL OR Id <> @ExcludeId)) THEN 1 ELSE 0 END;";

    public const string Insert =
        "INSERT INTO Api (ProjectId, Name, Method, Endpoint, Description, IsActive, CreatedDate) "
        + "OUTPUT INSERTED.Id VALUES (@ProjectId, @Name, @Method, @Endpoint, @Description, @IsActive, SYSUTCDATETIME());";

    public const string Update =
        "UPDATE Api SET Name = @Name, Method = @Method, Endpoint = @Endpoint, Description = @Description, "
        + "IsActive = @IsActive, UpdatedDate = SYSUTCDATETIME() WHERE Id = @Id;";

    public const string InsertMany =
        "INSERT INTO Api (ProjectId, Name, Method, Endpoint, Description, IsActive, CreatedDate) "
        + "VALUES (@ProjectId, @Name, @Method, @Endpoint, @Description, @IsActive, SYSUTCDATETIME());";

    public const string MethodEndpointKeys =
        "SELECT UPPER(a.Method) + '|' + a.Endpoint FROM Api a WHERE a.ProjectId = @ProjectId;";

    public const string Names = "SELECT a.Name FROM Api a WHERE a.ProjectId = @ProjectId;";

    public const string Delete = "DELETE FROM Api WHERE Id = @Id;";
}
