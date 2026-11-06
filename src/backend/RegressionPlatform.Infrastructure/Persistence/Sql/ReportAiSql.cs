namespace RegressionPlatform.Infrastructure.Persistence.Sql;

/// <summary>Parameterized SQL for the persisted report AI analysis (one row per report).</summary>
internal static class ReportAiSql
{
    /// <summary>The stored analysis for a report (provider + JSON + generated date), or none.</summary>
    public const string GetByReportId =
        "SELECT Provider, ContentJson, GeneratedDate "
        + "FROM ReportAiAnalysis WHERE ReportId = @ReportId;";

    /// <summary>
    /// Insert or refresh the report's analysis in place (bumps GeneratedDate). Returns the stored
    /// GeneratedDate so the caller can echo it back to the client.
    /// </summary>
    public const string Upsert =
        "UPDATE ReportAiAnalysis SET Provider = @Provider, ContentJson = @ContentJson, "
        + "GeneratedDate = SYSUTCDATETIME() WHERE ReportId = @ReportId; "
        + "IF @@ROWCOUNT = 0 "
        + "INSERT INTO ReportAiAnalysis (ReportId, Provider, ContentJson, GeneratedDate, CreatedDate) "
        + "VALUES (@ReportId, @Provider, @ContentJson, SYSUTCDATETIME(), SYSUTCDATETIME()); "
        + "SELECT GeneratedDate FROM ReportAiAnalysis WHERE ReportId = @ReportId;";
}
