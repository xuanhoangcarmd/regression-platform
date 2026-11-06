using MediatR;
using Microsoft.AspNetCore.Mvc;
using RegressionPlatform.API.Contracts;
using RegressionPlatform.Application.Features.ApiDefinition.Commands.CreateApi;
using RegressionPlatform.Application.Features.ApiDefinition.Commands.DeleteApi;
using RegressionPlatform.Application.Features.ApiDefinition.Commands.ImportApis;
using RegressionPlatform.Application.Features.ApiDefinition.Commands.UpdateApi;
using RegressionPlatform.Application.Features.ApiDefinition.Contracts;
using RegressionPlatform.Application.Features.ApiDefinition.Queries.GetApiById;
using RegressionPlatform.Application.Features.ApiDefinition.Queries.GetApis;
using RegressionPlatform.Shared.Pagination;

namespace RegressionPlatform.API.Controllers;

/// <summary>Manage API endpoint definitions.</summary>
[Route("api/apis")]
public sealed class ApisController(ISender sender) : ApiControllerBase
{
    /// <summary>List API definitions.</summary>
    /// <remarks>Supports search, status filter (isActive), sorting (sortBy/sortDirection), and pagination.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ApiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApis(
        [FromQuery] string projectId,
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
                new GetApisQuery(projectId, search, isActive, sortBy, sortDirection, pageNumber, pageSize),
                cancellationToken
            )
        );

    /// <summary>Get a single API definition by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApiById(int id, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new GetApiByIdQuery(id), cancellationToken));

    /// <summary>Create a new API definition.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateApi(
        [FromQuery] string projectId,
        [FromBody] CreateApiRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(
            new CreateApiCommand(projectId, request.Name, request.Method, request.Endpoint, request.Description, request.IsActive),
            cancellationToken
        );
        return HandleCreated(result, nameof(GetApiById), new { id = result.Value?.Id });
    }

    /// <summary>Update an existing API definition.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateApi(
        int id,
        [FromQuery] string projectId,
        [FromBody] UpdateApiRequest request,
        CancellationToken cancellationToken
    ) =>
        HandleResult(
            await sender.Send(
                new UpdateApiCommand(
                    projectId,
                    id,
                    request.Name,
                    request.Method,
                    request.Endpoint,
                    request.Description,
                    request.IsActive
                ),
                cancellationToken
            )
        );

    /// <summary>Import API definitions from an OpenAPI/Swagger URL.</summary>
    /// <remarks>Fetches the spec server-side, then bulk-creates new operations (skips duplicates by method + endpoint).</remarks>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ImportApisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportApis(
        [FromQuery] string projectId,
        [FromBody] ImportApisRequest request,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new ImportApisFromSwaggerCommand(projectId, request.SwaggerUrl), cancellationToken));

    /// <summary>Delete an API definition.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteApi(int id, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new DeleteApiCommand(id), cancellationToken));
}
