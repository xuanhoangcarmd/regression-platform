using MediatR;
using Microsoft.AspNetCore.Mvc;
using RegressionPlatform.API.Contracts;
using RegressionPlatform.Application.Features.Datasets.Commands.CreateDataset;
using RegressionPlatform.Application.Features.Datasets.Commands.DeleteDataset;
using RegressionPlatform.Application.Features.Datasets.Commands.UpdateDataset;
using RegressionPlatform.Application.Features.Datasets.Contracts;
using RegressionPlatform.Application.Features.Datasets.Queries.GetDatasetApis;
using RegressionPlatform.Application.Features.Datasets.Queries.GetDatasetById;
using RegressionPlatform.Application.Features.Datasets.Queries.GetDatasets;

namespace RegressionPlatform.API.Controllers;

/// <summary>Datasets — named collections of test requests, scoped to a project via <c>?projectId={code}</c>.</summary>
[Route("api/datasets")]
public sealed class DatasetsController(ISender sender) : ApiControllerBase
{
    /// <summary>List a project's datasets (with request + API counts).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DatasetListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDatasets([FromQuery] string projectId, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new GetDatasetsQuery(projectId), cancellationToken));

    /// <summary>Get one dataset (detail header / edit prefill).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DatasetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDatasetById(int id, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new GetDatasetByIdQuery(id), cancellationToken));

    /// <summary>List the APIs that have requests in a dataset (for the run-create API picker).</summary>
    [HttpGet("{id:int}/apis")]
    [ProducesResponseType(typeof(IReadOnlyList<DatasetApiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDatasetApis(int id, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new GetDatasetApisQuery(id), cancellationToken));

    /// <summary>Create a new dataset.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDataset(
        [FromQuery] string projectId,
        [FromBody] CreateDatasetBody body,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(new CreateDatasetCommand(projectId, body.Name, body.Description), cancellationToken);
        return HandleCreated(result, nameof(GetDatasetById), new { id = result.Value });
    }

    /// <summary>Update a dataset (rename / description / active state).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateDataset(
        int id,
        [FromBody] UpdateDatasetBody body,
        CancellationToken cancellationToken
    ) =>
        HandleResult(
            await sender.Send(new UpdateDatasetCommand(id, body.Name, body.Description, body.IsActive), cancellationToken)
        );

    /// <summary>Delete a dataset (blocked while it still has requests or runs).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteDataset(int id, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new DeleteDatasetCommand(id), cancellationToken));
}
