using RegressionPlatform.Application.Features.Report.Ai.Contracts;

namespace RegressionPlatform.Application.Features.Report.Ai;

/// <summary>
/// Caches a report's AI analysis so re-opening the tab doesn't re-bill the LLM. A report is an
/// immutable snapshot, so the cached result stays valid. Implemented in Infrastructure.
/// </summary>
public interface IReportAiCache
{
    ReportAiAnalysisResult? Get(long reportId);

    void Set(long reportId, ReportAiAnalysisResult result);
}
