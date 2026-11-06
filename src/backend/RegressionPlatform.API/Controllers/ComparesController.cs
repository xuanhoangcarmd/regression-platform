using MediatR;
using Microsoft.AspNetCore.Mvc;
using RegressionPlatform.API.Contracts;
using RegressionPlatform.Application.Features.Compare.Commands.CreateCompare;
using RegressionPlatform.Application.Features.Compare.Commands.DeleteCompare;
using RegressionPlatform.Application.Features.Compare.Commands.StartCompare;
using RegressionPlatform.Application.Features.Compare.Commands.StopCompare;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Application.Features.Compare.Queries.GetCompareApiStatistics;
using RegressionPlatform.Application.Features.Compare.Queries.GetCompareById;
using RegressionPlatform.Application.Features.Compare.Queries.GetCompareChangeSummary;
using RegressionPlatform.Application.Features.Compare.Queries.GetCompareDetailDiff;
using RegressionPlatform.Application.Features.Compare.Queries.GetCompareDetails;
using RegressionPlatform.Application.Features.Compare.Queries.GetCompareFullDetails;
using RegressionPlatform.Application.Features.Compare.Queries.GetCompareTestResults;
using RegressionPlatform.Application.Features.Compare.Queries.GetCompares;
using RegressionPlatform.Shared.Pagination;

namespace RegressionPlatform.API.Controllers;

/// <summary>Manage compares of two runs (scoped to a project via <c>?projectId={code}</c>).</summary>
[Route("api/compares")]
public sealed class ComparesController(ISender sender) : ApiControllerBase
{
    /// <summary>List a project's compares (newest first) for the History screen.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CompareJobListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompares([FromQuery] string projectId, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new GetComparesQuery(projectId), cancellationToken));

    /// <summary>Create a compare of two Completed runs (in Pending status). Start it separately.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CompareJobListItem), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCompare(
        [FromQuery] string projectId,
        [FromBody] CreateCompareRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(
            new CreateCompareCommand(projectId, request.RunAId, request.RunBId, request.ApiIds ?? [], request.BaselineMode, request.IgnoreOrder),
            cancellationToken
        );
        return HandleCreated(result, nameof(GetCompareById), new { projectId, id = result.Value?.Id });
    }

    /// <summary>Start a pending compare (queues it for background execution). Returns 204.</summary>
    [HttpPost("{id:long}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartCompare(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new StartCompareCommand(projectId, id), cancellationToken));

    /// <summary>Stop a running compare (marks it Cancelled and aborts the worker). Returns 204.</summary>
    [HttpPost("{id:long}/stop")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StopCompare(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new StopCompareCommand(projectId, id), cancellationToken));

    /// <summary>Delete a compare and all its data (blocked while Running or if a report was generated). Returns 204.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCompare(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new DeleteCompareCommand(projectId, id), cancellationToken));

    /// <summary>Get the detail header (job + result summary) for a compare.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(CompareSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompareById(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetCompareByIdQuery(projectId, id), cancellationToken));

    /// <summary>Distinct change patterns of a compare (compact summary for review / AI).</summary>
    [HttpGet("{id:long}/change-summary")]
    [ProducesResponseType(typeof(CompareChangeSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChangeSummary(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetCompareChangeSummaryQuery(projectId, id), cancellationToken));

    /// <summary>Per-API breakdown ("Change by API") for a compare.</summary>
    [HttpGet("{id:long}/api-statistics")]
    [ProducesResponseType(typeof(IReadOnlyList<CompareApiStatistic>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompareApiStatistics(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetCompareApiStatisticsQuery(projectId, id), cancellationToken));

    /// <summary>Paged per-request detail list with search + result filter and sorting.</summary>
    [HttpGet("{id:long}/details")]
    [ProducesResponseType(typeof(PagedResult<CompareDetailListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompareDetails(
        long id,
        [FromQuery] string projectId,
        [FromQuery] string? search,
        [FromQuery] string? result,
        CancellationToken cancellationToken,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] int? apiId = null,
        [FromQuery] string? changeType = null,
        [FromQuery] string? changePath = null
    ) =>
        HandleResult(
            await sender.Send(
                new GetCompareDetailsQuery(
                    projectId, id, search, result, pageNumber, pageSize, sortBy, sortDirection, apiId, changeType, changePath
                ),
                cancellationToken
            )
        );

    /// <summary>All per-request details (identity + result + raw field diff) — for the full Excel export.</summary>
    [HttpGet("{id:long}/details/full")]
    [ProducesResponseType(typeof(IReadOnlyList<CompareFullDetailItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompareFullDetails(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetCompareFullDetailsQuery(projectId, id), cancellationToken));

    /// <summary>
    /// Every request with its input, BOTH runs' stored responses and the compare outcome — for the
    /// report's "Test Results" (Pass/Fail) Excel export.
    /// </summary>
    [HttpGet("{id:long}/details/test-results")]
    [ProducesResponseType(typeof(IReadOnlyList<CompareTestResultItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompareTestResults(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetCompareTestResultsQuery(projectId, id), cancellationToken));

    /// <summary>Full diff for one request (field diff + both runs' bodies) for the side-by-side viewer.</summary>
    [HttpGet("{id:long}/details/{requestId:long}")]
    [ProducesResponseType(typeof(CompareDetailDiffResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompareDetailDiff(
        long id,
        long requestId,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetCompareDetailDiffQuery(projectId, id, requestId), cancellationToken));
}
