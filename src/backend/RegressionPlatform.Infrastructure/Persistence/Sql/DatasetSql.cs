namespace RegressionPlatform.Infrastructure.Persistence.Sql;

/// <summary>Parameterized SQL for datasets (named request collections).</summary>
internal static class DatasetSql
{
    // Column order matches DatasetListItemResponse (Dapper binds positionally).
    public const string GetByProject =
        "SELECT d.Id, d.Name, d.Description, d.IsActive, "
        + "(SELECT COUNT(1) FROM ApiRequest r WHERE r.DatasetId = d.Id AND r.IsDeleted = 0) AS RequestCount, "
        + "(SELECT COUNT(DISTINCT r.ApiId) FROM ApiRequest r WHERE r.DatasetId = d.Id AND r.IsDeleted = 0) AS ApiCount "
        + "FROM Dataset d WHERE d.ProjectId = @ProjectId ORDER BY d.Name;";

    public const string GetById = "SELECT Id, Name, Description, IsActive FROM Dataset WHERE Id = @Id;";

    // Column order matches DatasetApiResponse (Dapper binds positionally).
    public const string GetApis =
        "SELECT a.Id, a.Name, a.Method, a.Endpoint, COUNT(1) AS RequestCount "
        + "FROM ApiRequest r INNER JOIN Api a ON a.Id = r.ApiId "
        + "WHERE r.DatasetId = @DatasetId AND r.IsDeleted = 0 "
        + "GROUP BY a.Id, a.Name, a.Method, a.Endpoint ORDER BY a.Name;";

    public const string GetEntityById =
        "SELECT Id, ProjectId, Name, Description, IsActive, CreatedDate, UpdatedDate FROM Dataset WHERE Id = @Id;";

    public const string ExistsByName =
        "SELECT CASE WHEN EXISTS ("
        + "SELECT 1 FROM Dataset WHERE ProjectId = @ProjectId AND Name = @Name AND (@ExcludeId IS NULL OR Id <> @ExcludeId)"
        + ") THEN 1 ELSE 0 END;";

    /// <summary>
    /// 1 if any run still references the dataset (blocks deletion — run history would break).
    /// The dataset's own requests do NOT block: they belong to it and are deleted with it.
    /// </summary>
    public const string HasRuns =
        "SELECT CASE WHEN EXISTS (SELECT 1 FROM Run WHERE DatasetId = @Id) THEN 1 ELSE 0 END;";

    public const string Insert =
        "INSERT INTO Dataset (ProjectId, Name, Description, IsActive, CreatedDate) "
        + "OUTPUT INSERTED.Id "
        + "VALUES (@ProjectId, @Name, @Description, @IsActive, SYSUTCDATETIME());";

    public const string Update =
        "UPDATE Dataset SET Name = @Name, Description = @Description, IsActive = @IsActive, "
        + "UpdatedDate = SYSUTCDATETIME() WHERE Id = @Id;";

    /// <summary>
    /// Deletes a dataset and its requests. Only ever called for datasets with NO runs (the handler
    /// blocks otherwise), so no RunResponse/CompareDetail rows can still reference those requests.
    /// </summary>
    public const string Delete =
        "DELETE FROM ApiRequest WHERE DatasetId = @Id;\n" + "DELETE FROM Dataset WHERE Id = @Id;";
}
