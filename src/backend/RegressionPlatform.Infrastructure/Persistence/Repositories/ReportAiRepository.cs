using System.Text.Json;
using Dapper;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.Report.Ai;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;
using RegressionPlatform.Infrastructure.Persistence.Sql;

namespace RegressionPlatform.Infrastructure.Persistence.Repositories;

/// <summary>Dapper data access for the persisted report AI analysis (JSON payload + provider + date).</summary>
public sealed class ReportAiRepository(IDbConnectionFactory connectionFactory) : IReportAiRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ReportAiAnalysisResult?> GetByReportIdAsync(long reportId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<StoredRow>(
            new CommandDefinition(ReportAiSql.GetByReportId, new { ReportId = reportId }, cancellationToken: cancellationToken)
        );
        if (row is null)
        {
            return null;
        }

        var result = JsonSerializer.Deserialize<ReportAiAnalysisResult>(row.ContentJson, JsonOptions);
        return result is null ? null : result with { Provider = row.Provider, GeneratedAt = row.GeneratedDate };
    }

    public async Task<DateTime> UpsertAsync(long reportId, ReportAiAnalysisResult result, CancellationToken cancellationToken)
    {
        var contentJson = JsonSerializer.Serialize(result, JsonOptions);
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<DateTime>(
            new CommandDefinition(
                ReportAiSql.Upsert,
                new { ReportId = reportId, result.Provider, ContentJson = contentJson },
                cancellationToken: cancellationToken
            )
        );
    }

    private sealed record StoredRow(string Provider, string ContentJson, DateTime GeneratedDate);
}
