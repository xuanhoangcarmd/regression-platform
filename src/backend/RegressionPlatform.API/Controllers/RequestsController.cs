using MediatR;
using Microsoft.AspNetCore.Mvc;
using RegressionPlatform.API.Contracts;
using RegressionPlatform.Application.Features.RequestPool.Commands.CreateApiRequest;
using RegressionPlatform.Application.Features.RequestPool.Commands.DeleteApiRequest;
using RegressionPlatform.Application.Features.RequestPool.Commands.DeleteApiRequestsByApi;
using RegressionPlatform.Application.Features.RequestPool.Commands.ImportApiRequests;
using RegressionPlatform.Application.Features.RequestPool.Commands.UpdateApiRequest;
using RegressionPlatform.Application.Features.RequestPool.Contracts;
using RegressionPlatform.Application.Features.RequestPool.Queries.GetApiRequestById;
using RegressionPlatform.Application.Features.RequestPool.Queries.GetApiRequests;
using RegressionPlatform.Shared.Pagination;

namespace RegressionPlatform.API.Controllers;

/// <summary>The Request Pool — reusable API requests.</summary>
[Route("api/requests")]
public sealed class RequestsController(ISender sender) : ApiControllerBase
{
    /// <summary>List a dataset's requests.</summary>
    /// <remarks>Supports filtering by API (apiId), title search, status (isActive), sorting, and pagination.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ApiRequestListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRequests(
        [FromQuery] int datasetId,
        [FromQuery] int? apiId,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        CancellationToken cancellationToken,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20
    ) =>
        HandleResult(
            await sender.Send(
                new GetApiRequestsQuery(datasetId, apiId, search, isActive, sortBy, sortDirection, pageNumber, pageSize),
                cancellationToken
            )
        );

    /// <summary>Get a single pooled request by id (full detail).</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiRequestDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRequestById(long id, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new GetApiRequestByIdQuery(id), cancellationToken));

    /// <summary>Create a new request in the pool.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(long), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRequest(
        [FromBody] CreateApiRequestBody body,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(
            new CreateApiRequestCommand(
                body.DatasetId,
                body.ApiId,
                body.Title,
                body.Description,
                body.HeadersJson,
                body.QueryStringJson,
                body.RequestBody
            ),
            cancellationToken
        );
        return HandleCreated(result, nameof(GetRequestById), new { id = result.Value });
    }

    /// <summary>Update a request in place.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRequest(
        long id,
        [FromBody] UpdateApiRequestBody body,
        CancellationToken cancellationToken
    ) =>
        HandleResult(
            await sender.Send(
                new UpdateApiRequestCommand(
                    id,
                    body.ApiId,
                    body.Title,
                    body.Description,
                    body.HeadersJson,
                    body.QueryStringJson,
                    body.RequestBody
                ),
                cancellationToken
            )
        );

    /// <summary>Bulk-import requests for an API from an Excel (.xlsx) file.</summary>
    /// <remarks>
    /// Each data row becomes one request; column headers are the field names. Fields map to the body
    /// (non-GET) or query string (GET) based on the API's method. A "Title" column names each request; an optional "Description" column is stored as its description.
    /// </remarks>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportApiRequestsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImportRequests(
        [FromForm] int datasetId,
        [FromForm] int apiId,
        IFormFile file,
        CancellationToken cancellationToken
    )
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(nameof(file), "Please select an Excel file (.xlsx).");
            return ValidationProblem(ModelState);
        }

        await using var stream = file.OpenReadStream();
        return HandleResult(await sender.Send(new ImportApiRequestsCommand(datasetId, apiId, stream), cancellationToken));
    }

    /// <summary>Archive a request (soft-delete; preserves history).</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRequest(long id, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new DeleteApiRequestCommand(id), cancellationToken));

    /// <summary>Delete ALL requests of an API within a dataset (plus their captured responses). Returns the count.</summary>
    [HttpDelete("by-api")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteRequestsByApi(
        [FromQuery] int datasetId,
        [FromQuery] int apiId,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new DeleteApiRequestsByApiCommand(datasetId, apiId), cancellationToken));
}
