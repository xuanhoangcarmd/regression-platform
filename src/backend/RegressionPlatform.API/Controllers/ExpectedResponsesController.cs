using MediatR;
using Microsoft.AspNetCore.Mvc;
using RegressionPlatform.API.Contracts;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Application.Features.Runs.ExpectedResponses.GetExpectedApis;
using RegressionPlatform.Application.Features.Runs.ExpectedResponses.GetExpectedDetails;
using RegressionPlatform.Application.Features.Runs.ExpectedResponses.GetExpectedRequests;
using RegressionPlatform.Application.Features.Runs.ExpectedResponses.RestoreExpectedResponses;
using RegressionPlatform.Application.Features.Runs.ExpectedResponses.SaveExpectedResponses;

namespace RegressionPlatform.API.Controllers;

/// <summary>
/// Expected (edited) responses of a run — what the run's responses SHOULD be. A compare in
/// 'Expected' baseline mode uses them for Run A. Scoped to a project via <c>?projectId={code}</c>.
/// </summary>
[Route("api/runs/{runId:long}/expected")]
public sealed class ExpectedResponsesController(ISender sender) : ApiControllerBase
{
    /// <summary>APIs of the run with request + edited counts.</summary>
    [HttpGet("apis")]
    [ProducesResponseType(typeof(IReadOnlyList<ExpectedApiItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApis(long runId, [FromQuery] string projectId, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new GetExpectedApisQuery(projectId, runId), cancellationToken));

    /// <summary>The run's executed requests of one API (no bodies) with their edited flag.</summary>
    [HttpGet("requests")]
    [ProducesResponseType(typeof(IReadOnlyList<ExpectedRequestItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRequests(
        long runId,
        [FromQuery] string projectId,
        [FromQuery] int apiId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetExpectedRequestsQuery(projectId, runId, apiId), cancellationToken));

    /// <summary>
    /// Original + edited bodies for up to 1000 requests. POST because the id list can be long
    /// (bulk-edit preview); it does not change anything.
    /// </summary>
    [HttpPost("details")]
    [ProducesResponseType(typeof(IReadOnlyList<ExpectedResponseDetail>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(
        long runId,
        [FromQuery] string projectId,
        [FromBody] ExpectedRequestIdsBody body,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetExpectedDetailsQuery(projectId, runId, body.RequestIds ?? []), cancellationToken));

    /// <summary>Save (upsert) up to 1000 edited responses in one transaction. Returns how many were saved.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Save(
        long runId,
        [FromQuery] string projectId,
        [FromBody] SaveExpectedResponsesBody body,
        CancellationToken cancellationToken
    )
    {
        var items = (body.Items ?? [])
            .Select(item => new ExpectedResponseSaveItem(item.RequestId, item.StatusCode, item.ResponseJson, item.Reason))
            .ToList();
        return HandleResult(await sender.Send(new SaveExpectedResponsesCommand(projectId, runId, items), cancellationToken));
    }

    /// <summary>Delete the edited versions (compares use the original again). Returns how many were removed.</summary>
    [HttpPost("restore")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Restore(
        long runId,
        [FromQuery] string projectId,
        [FromBody] ExpectedRequestIdsBody body,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new RestoreExpectedResponsesCommand(projectId, runId, body.RequestIds ?? []), cancellationToken));
}
