namespace RegressionPlatform.Infrastructure.Persistence.Sql;

/// <summary>
/// SQL for a run's expected (edited) responses (<c>RunResponseOverride</c>). Every query starts from
/// <c>RunResponse WHERE RunId = @RunId</c> (seek on UX_Run_Request) and LEFT JOINs the override by
/// its unique (RunId, RequestId) index.
/// </summary>
internal static class ExpectedResponseSql
{
    private const string RunResponses =
        "FROM RunResponse rr "
        + "INNER JOIN ApiRequest req ON req.Id = rr.RequestId "
        + "LEFT JOIN RunResponseOverride o ON o.RunId = rr.RunId AND o.RequestId = rr.RequestId ";

    /// <summary>Per-API request + edited counts (column order matches ExpectedApiItem).</summary>
    public const string Apis =
        "SELECT a.Id AS ApiId, a.Name AS ApiName, a.Method, a.Endpoint, COUNT(1) AS RequestCount, COUNT(o.Id) AS EditedCount "
        + RunResponses
        + "INNER JOIN Api a ON a.Id = req.ApiId "
        + "WHERE rr.RunId = @RunId "
        + "GROUP BY a.Id, a.Name, a.Method, a.Endpoint ORDER BY a.Name;";

    /// <summary>One API's requests in the run, no bodies (column order matches ExpectedRequestItem).</summary>
    public const string Requests =
        "SELECT rr.RequestId, req.ApiId, req.Title, req.Description, "
        + "CASE WHEN o.Id IS NOT NULL AND o.StatusCode IS NOT NULL THEN o.StatusCode ELSE rr.StatusCode END AS StatusCode, "
        + "CAST(CASE WHEN o.Id IS NULL THEN 0 ELSE 1 END AS BIT) AS IsEdited, "
        + "COALESCE(o.UpdatedBy, o.CreatedBy) AS EditedBy, COALESCE(o.UpdatedDate, o.CreatedDate) AS EditedDate "
        + RunResponses
        + "WHERE rr.RunId = @RunId AND req.ApiId = @ApiId "
        + "ORDER BY req.Title, rr.RequestId;";

    /// <summary>Original + edited bodies for a batch of requests (≤ 500 ids per call — Dapper expands IN).</summary>
    public const string Details =
        "SELECT rr.RequestId, req.ApiId, req.Title, rr.StatusCode, rr.ResponseJson, rr.ErrorMessage, "
        + "o.Id AS OverrideId, o.StatusCode AS OverrideStatusCode, o.ResponseJson AS OverrideResponseJson, "
        + "o.Reason AS OverrideReason, COALESCE(o.UpdatedBy, o.CreatedBy) AS OverrideUpdatedBy, "
        + "COALESCE(o.UpdatedDate, o.CreatedDate) AS OverrideUpdatedDate "
        + RunResponses
        + "WHERE rr.RunId = @RunId AND rr.RequestId IN @RequestIds;";

    public const string CountResponses =
        "SELECT COUNT(1) FROM RunResponse WHERE RunId = @RunId AND RequestId IN @RequestIds;";

    // ----- Save: bulk-copy the batch into a session temp table, then one set-based upsert -----

    public const string CreateStaging =
        "CREATE TABLE #ExpectedItems ("
        + "RequestId BIGINT NOT NULL PRIMARY KEY, StatusCode INT NULL, ResponseJson NVARCHAR(MAX) NOT NULL, "
        + "ResponseHash CHAR(64) NOT NULL, ResponseOrderedHash CHAR(64) NOT NULL, Reason NVARCHAR(500) NULL);";

    public const string StagingTable = "#ExpectedItems";

    /// <summary>Update existing edits, insert new ones (UPDLOCK/HOLDLOCK: no duplicate on concurrent saves).</summary>
    public const string Upsert =
        "UPDATE o SET o.StatusCode = i.StatusCode, o.ResponseJson = i.ResponseJson, o.ResponseHash = i.ResponseHash, "
        + "o.ResponseOrderedHash = i.ResponseOrderedHash, "
        + "o.Reason = i.Reason, o.UpdatedBy = @UserName, o.UpdatedDate = SYSUTCDATETIME() "
        + "FROM RunResponseOverride o WITH (UPDLOCK, HOLDLOCK) "
        + "INNER JOIN #ExpectedItems i ON i.RequestId = o.RequestId "
        + "WHERE o.RunId = @RunId;\n"
        + "INSERT INTO RunResponseOverride (RunId, RequestId, StatusCode, ResponseJson, ResponseHash, ResponseOrderedHash, Reason, CreatedBy, CreatedDate) "
        + "SELECT @RunId, i.RequestId, i.StatusCode, i.ResponseJson, i.ResponseHash, i.ResponseOrderedHash, i.Reason, @UserName, SYSUTCDATETIME() "
        + "FROM #ExpectedItems i "
        + "WHERE NOT EXISTS (SELECT 1 FROM RunResponseOverride o WITH (UPDLOCK, HOLDLOCK) "
        + "WHERE o.RunId = @RunId AND o.RequestId = i.RequestId);\n"
        + "DROP TABLE #ExpectedItems;";

    public const string Delete = "DELETE FROM RunResponseOverride WHERE RunId = @RunId AND RequestId IN @RequestIds;";
}
