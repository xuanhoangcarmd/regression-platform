using MediatR;
using Microsoft.AspNetCore.Mvc;
using RegressionPlatform.API.Contracts;
using RegressionPlatform.Application.Features.Runs.Commands.CreateRun;
using RegressionPlatform.Application.Features.Runs.Commands.DeleteRun;
using RegressionPlatform.Application.Features.Runs.Commands.RerunRequest;
using RegressionPlatform.Application.Features.Runs.Commands.StartRun;
using RegressionPlatform.Application.Features.Runs.Commands.StopRun;
using RegressionPlatform.Application.Features.Runs.Commands.UpdateRun;
using RegressionPlatform.Application.Features.Runs.Queries.GetRunForEdit;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Application.Features.Runs.Queries.GetRunById;
using RegressionPlatform.Application.Features.Runs.Queries.GetRunExport;
using RegressionPlatform.Application.Features.Runs.Queries.GetRunResponseDetail;
using RegressionPlatform.Application.Features.Runs.Queries.GetRunResponses;
using RegressionPlatform.Application.Features.Runs.Queries.GetRuns;
using RegressionPlatform.Application.Features.Runs.Queries.GetRunsProgress;
using RegressionPlatform.Shared.Pagination;

namespace RegressionPlatform.API.Controllers;

/// <summary>Manage regression runs (scoped to a project via <c>?projectId={code}</c>).</summary>
[Route("api/runs")]
public sealed class RunsController(ISender sender) : ApiControllerBase
{
    /// <summary>List a project's runs (newest first).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RunResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRuns([FromQuery] string projectId, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new GetRunsQuery(projectId), cancellationToken));

    /// <summary>Every executed request of a run (API, input, full response, timing) — for the Excel export.</summary>
    [HttpGet("{id:long}/export")]
    [ProducesResponseType(typeof(IReadOnlyList<RunResponseExportRow>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunExport(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetRunExportQuery(projectId, id), cancellationToken));

    /// <summary>
    /// Live progress for specific runs (poll target for the list). Pass only the ids of runs currently
    /// visible and executing via <c>?ids=1&amp;ids=2</c> — keeps the poll cheap regardless of list size.
    /// </summary>
    [HttpGet("progress")]
    [ProducesResponseType(typeof(IReadOnlyList<RunProgressResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunsProgress(
        [FromQuery] string projectId,
        [FromQuery] long[] ids,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetRunsProgressQuery(projectId, ids ?? []), cancellationToken));

    /// <summary>Create a new (pending) run. Does not execute it yet.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(RunResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateRun(
        [FromQuery] string projectId,
        [FromBody] CreateRunRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(
            new CreateRunCommand(
                projectId,
                request.DatasetId,
                request.Title,
                request.Description,
                request.Environment,
                request.RunType,
                request.RequestLimitPerApi,
                request.ApiIds ?? [],
                request.MaxParallelApis ?? 1,
                request.MaxParallelRequestsPerApi ?? 1
            ),
            cancellationToken
        );
        return HandleCreated(result, nameof(GetRuns), new { projectId });
    }

    /// <summary>Get full detail (Summary) for a run.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(RunDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunById(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetRunByIdQuery(projectId, id), cancellationToken));

    /// <summary>List a run's executed requests (Requests tab) with search + status filter and paging.</summary>
    [HttpGet("{id:long}/responses")]
    [ProducesResponseType(typeof(PagedResult<RunResponseListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunResponses(
        long id,
        [FromQuery] string projectId,
        [FromQuery] string? search,
        [FromQuery] string? status,
        CancellationToken cancellationToken,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] int? apiId = null
    ) =>
        HandleResult(
            await sender.Send(
                new GetRunResponsesQuery(projectId, id, search, status, pageNumber, pageSize, sortBy, sortDirection, apiId),
                cancellationToken
            )
        );

    /// <summary>Re-run a single request inside a run and update its stored result. Returns 204.</summary>
    [HttpPost("{id:long}/responses/{requestId:long}/rerun")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RerunRequest(
        long id,
        long requestId,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new RerunRequestCommand(projectId, id, requestId), cancellationToken));

    /// <summary>Full detail of one executed request in a run (Results tab): body, headers, and the sent request.</summary>
    [HttpGet("{id:long}/responses/{requestId:long}")]
    [ProducesResponseType(typeof(RunResponseDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunResponseDetail(
        long id,
        long requestId,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetRunResponseDetailQuery(projectId, id, requestId), cancellationToken));

    /// <summary>Start a pending run (queues it for background execution). Returns 204.</summary>
    [HttpPost("{id:long}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartRun(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new StartRunCommand(projectId, id), cancellationToken));

    /// <summary>Stop a running run (marks it Cancelled and aborts the background execution). Returns 204.</summary>
    [HttpPost("{id:long}/stop")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StopRun(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new StopRunCommand(projectId, id), cancellationToken));

    /// <summary>Delete a run and ALL data derived from it (responses, compares, reports). Returns 204.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRun(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new DeleteRunCommand(projectId, id), cancellationToken));

    /// <summary>Editable fields of a pending run (to prefill the edit form).</summary>
    [HttpGet("{id:long}/edit")]
    [ProducesResponseType(typeof(RunEditResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunForEdit(
        long id,
        [FromQuery] string projectId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetRunForEditQuery(projectId, id), cancellationToken));

    /// <summary>Update a pending run in place (only Pending runs are editable).</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(RunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateRun(
        long id,
        [FromQuery] string projectId,
        [FromBody] CreateRunRequest request,
        CancellationToken cancellationToken
    ) =>
        HandleResult(
            await sender.Send(
                new UpdateRunCommand(
                    projectId,
                    id,
                    request.DatasetId,
                    request.Title,
                    request.Description,
                    request.Environment,
                    request.RunType,
                    request.RequestLimitPerApi,
                    request.ApiIds ?? [],
                    request.MaxParallelApis ?? 1,
                    request.MaxParallelRequestsPerApi ?? 1
                ),
                cancellationToken
            )
        );
}

