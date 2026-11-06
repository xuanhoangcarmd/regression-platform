using RegressionPlatform.Application.Features.Report.Ai.Contracts;

namespace RegressionPlatform.Application.Features.Report.Ai;

/// <summary>Persists the AI analysis of a report (one row per report). Implemented in Infrastructure.</summary>
public interface IReportAiRepository
{
    /// <summary>The stored analysis for a report (with its GeneratedAt), or null if none yet.</summary>
    Task<ReportAiAnalysisResult?> GetByReportIdAsync(long reportId, CancellationToken cancellationToken);

    /// <summary>Insert or replace the report's analysis (bumps GeneratedDate). Returns the stored GeneratedDate.</summary>
    Task<DateTime> UpsertAsync(long reportId, ReportAiAnalysisResult result, CancellationToken cancellationToken);
}
