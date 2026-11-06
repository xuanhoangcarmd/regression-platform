using RegressionPlatform.Application.Features.Report.Ai.Contracts;

namespace RegressionPlatform.Application.Features.Report.Ai;

/// <summary>
/// Short-lived cache of Ask-AI answers, keyed by report + question, so repeat questions (e.g. clicking
/// the same example prompt) don't re-bill the LLM. A report is an immutable snapshot, so a cached
/// answer stays valid for the window. Implemented in Infrastructure.
/// </summary>
public interface IReportAiAskCache
{
    ReportAiAskResponse? Get(long reportId, string question);

    void Set(long reportId, string question, ReportAiAskResponse response);
}
