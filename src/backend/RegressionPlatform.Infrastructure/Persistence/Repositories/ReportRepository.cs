using Dapper;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.Report;
using RegressionPlatform.Application.Features.Report.Contracts;
using RegressionPlatform.Infrastructure.Persistence.Sql;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Infrastructure.Persistence.Repositories;

/// <summary>Dapper data access for reports (Reports list + detail; 1 report per compare).</summary>
public sealed class ReportRepository(IDbConnectionFactory connectionFactory) : IReportRepository
{
    public async Task<long> UpsertAsync(DomainEntities.Report report, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                ReportSql.Upsert,
                new
                {
                    report.ProjectId,
                    report.CompareJobId,
                    report.CompareResultId,
                    report.Name,
                    report.Status,
                    report.RegressionScore,
                    report.RiskLevel,
                    report.TotalRequest,
                    report.UnchangedCount,
                    report.ChangedCount,
                    report.AddedCount,
                    report.RemovedCount,
                    report.FailedCount,
                    report.TotalApis,
                    report.BreakingApiCount,
                },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task<IReadOnlyList<ReportListItem>> GetByProjectAsync(int projectId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<ReportListItem>(
            new CommandDefinition(ReportSql.GetByProject, new { ProjectId = projectId }, cancellationToken: cancellationToken)
        );
        return rows.ToList();
    }

    public async Task<ReportSummary?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ReportSummary>(
            new CommandDefinition(ReportSql.GetById, new { Id = id }, cancellationToken: cancellationToken)
        );
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(
            new CommandDefinition(ReportSql.Delete, new { Id = id }, transaction, cancellationToken: cancellationToken)
        );
        transaction.Commit();
    }

    public async Task<IReadOnlyList<ReportPerfRow>> GetPerformanceRowsAsync(
        long runAId,
        long runBId,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<ReportPerfRow>(
            new CommandDefinition(
                ReportSql.GetPerformanceRows,
                new { RunAId = runAId, RunBId = runBId },
                cancellationToken: cancellationToken
            )
        );
        return rows.ToList();
    }

    public async Task<(IReadOnlyList<ReportRequestTiming> PerRequest, IReadOnlyList<ReportApiTiming> PerApi)> GetPerformanceAsync(
        long runAId,
        long runBId,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(
                ReportSql.GetPerformance,
                new { RunAId = runAId, RunBId = runBId },
                cancellationToken: cancellationToken
            )
        );
        var perRequest = (await multi.ReadAsync<ReportRequestTiming>()).ToList();
        var perApi = (await multi.ReadAsync<ReportApiTiming>()).ToList();
        return (perRequest, perApi);
    }
}
