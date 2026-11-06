using MediatR;
using Microsoft.AspNetCore.Mvc;
using RegressionPlatform.API.Contracts;
using RegressionPlatform.Application.Features.IgnoreRule.Commands.CreateIgnoreRule;
using RegressionPlatform.Application.Features.IgnoreRule.Commands.DeleteIgnoreRule;
using RegressionPlatform.Application.Features.IgnoreRule.Commands.UpdateIgnoreRule;
using RegressionPlatform.Application.Features.IgnoreRule.Contracts;
using RegressionPlatform.Application.Features.IgnoreRule.Queries.GetIgnoreRuleById;
using RegressionPlatform.Application.Features.IgnoreRule.Queries.GetIgnoreRules;

namespace RegressionPlatform.API.Controllers;

/// <summary>Manage compare ignore rules (fields skipped during response comparison).</summary>
[Route("api/ignore-rules")]
public sealed class IgnoreRulesController(ISender sender) : ApiControllerBase
{
    /// <summary>List a project's ignore rules.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<IgnoreRuleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIgnoreRules([FromQuery] string projectId, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new GetIgnoreRulesQuery(projectId), cancellationToken));

    /// <summary>Get a single ignore rule by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(IgnoreRuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIgnoreRuleById(int id, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new GetIgnoreRuleByIdQuery(id), cancellationToken));

    /// <summary>Create a new ignore rule.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(IgnoreRuleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateIgnoreRule(
        [FromQuery] string projectId,
        [FromBody] CreateIgnoreRuleRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(
            new CreateIgnoreRuleCommand(
                projectId,
                request.JsonPath,
                request.Description,
                request.Enabled,
                request.RuleType ?? "Ignore",
                request.ApiIds ?? [],
                request.Delimiter
            ),
            cancellationToken
        );
        return HandleCreated(result, nameof(GetIgnoreRuleById), new { id = result.Value?.Id });
    }

    /// <summary>Update an existing ignore rule.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateIgnoreRule(
        int id,
        [FromBody] UpdateIgnoreRuleRequest request,
        CancellationToken cancellationToken
    ) =>
        HandleResult(
            await sender.Send(
                new UpdateIgnoreRuleCommand(
                    id,
                    request.JsonPath,
                    request.Description,
                    request.Enabled,
                    request.RuleType ?? "Ignore",
                    request.ApiIds ?? [],
                    request.Delimiter
                ),
                cancellationToken
            )
        );

    /// <summary>Delete an ignore rule.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteIgnoreRule(int id, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new DeleteIgnoreRuleCommand(id), cancellationToken));
}
