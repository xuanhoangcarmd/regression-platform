using MediatR;
using Microsoft.AspNetCore.Mvc;
using RegressionPlatform.API.Contracts;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;
using RegressionPlatform.Application.Features.Report.Commands.AskReportAiQuestion;
using RegressionPlatform.Application.Features.Report.Commands.DeleteReport;
using RegressionPlatform.Application.Features.Report.Commands.GenerateReport;
using RegressionPlatform.Application.Features.Report.Commands.RegenerateReportAiAnalysis;
using RegressionPlatform.Application.Features.Report.Contracts;
using RegressionPlatform.Application.Features.Report.Queries.GetReportAiAnalysis;
using RegressionPlatform.Application.Features.Report.Queries.GetReportById;
using RegressionPlatform.Application.Features.Report.Queries.GetReportPerformance;
using RegressionPlatform.Application.Features.Report.Queries.GetReportPerformanceStats;
using RegressionPlatform.Application.Features.Report.Queries.GetReports;
using RegressionPlatform.Application.Features.Report.Queries.GetStoredReportAiAnalysis;

namespace RegressionPlatform.API.Controllers;

/// <summary>Manage regression reports (scoped to a project via <c>?projectId={code}</c>). 1 report = 1 compare.</summary>
[Route("api/reports")]
public sealed class ReportsController(ISender sender) : ApiControllerBase
{
    /// <summary>List a project's reports (newest first) for the Reports screen.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReportListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReports([FromQuery] string projectId, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new GetReportsQuery(projectId), cancellationToken));

    /// <summary>Generate (or regenerate, in place) the report for a Completed compare.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReportListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateReport(
        [FromQuery] string projectId,
        [FromBody] GenerateReportRequest request,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GenerateReportCommand(projectId, request.CompareId), cancellationToken));

    /// <summary>Delete a report and its stored AI analysis (the compare/runs are untouched). Returns 204.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReport(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new DeleteReportCommand(projectId, id), cancellationToken));

    /// <summary>Get a report's detail (Summary tab): snapshot header + the compare's per-API breakdown.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ReportDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReportById(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetReportByIdQuery(projectId, id), cancellationToken));

    /// <summary>Response-time comparison (Run A vs Run B) for the report's Distribution tab charts.</summary>
    [HttpGet("{id:long}/performance")]
    [ProducesResponseType(typeof(ReportPerformanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReportPerformance(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetReportPerformanceQuery(projectId, id), cancellationToken));

    /// <summary>Extra performance statistics (latency histogram + per-API p95/max/size) for the Distribution tab.</summary>
    [HttpGet("{id:long}/performance-stats")]
    [ProducesResponseType(typeof(ReportPerformanceStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReportPerformanceStats(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetReportPerformanceStatsQuery(projectId, id), cancellationToken));

    /// <summary>Run the configured AI provider over the report (AI Analysis tab). Cached per report.</summary>
    [HttpGet("{id:long}/ai-analysis")]
    [ProducesResponseType(typeof(ReportAiAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReportAiAnalysis(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetReportAiAnalysisQuery(projectId, id), cancellationToken));

    /// <summary>The already-stored AI analysis (no generation); 404 if none has been generated yet.</summary>
    [HttpGet("{id:long}/ai-analysis/stored")]
    [ProducesResponseType(typeof(ReportAiAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStoredReportAiAnalysis(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetStoredReportAiAnalysisQuery(projectId, id), cancellationToken));

    /// <summary>Answer a single user question about the report ("Ask AI"), scoped to its data. Not persisted.</summary>
    [HttpPost("{id:long}/ai-analysis/ask")]
    [ProducesResponseType(typeof(ReportAiAskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AskReportAiQuestion(
        long id,
        [FromQuery] string projectId,
        [FromBody] AskReportAiQuestionRequest request,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new AskReportAiQuestionCommand(projectId, id, request.Question), cancellationToken));

    /// <summary>Force a re-run of the AI provider for the report (the "Regenerate" button); persists in place.</summary>
    [HttpPost("{id:long}/ai-analysis/regenerate")]
    [ProducesResponseType(typeof(ReportAiAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegenerateReportAiAnalysis(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new RegenerateReportAiAnalysisCommand(projectId, id), cancellationToken));
}
