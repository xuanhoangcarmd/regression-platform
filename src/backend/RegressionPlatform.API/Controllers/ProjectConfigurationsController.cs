using MediatR;
using Microsoft.AspNetCore.Mvc;
using RegressionPlatform.API.Contracts;
using RegressionPlatform.Application.Features.ProjectConfiguration.Commands.SaveEnvironmentConfiguration;
using RegressionPlatform.Application.Features.ProjectConfiguration.Contracts;
using RegressionPlatform.Application.Features.ProjectConfiguration.Queries.GetProjectConfigurations;

namespace RegressionPlatform.API.Controllers;

/// <summary>Manage per-project environment configuration (Base URL + headers).</summary>
[Route("api/projects/{code}/configurations")]
public sealed class ProjectConfigurationsController(ISender sender) : ApiControllerBase
{
    /// <summary>Get all environments' configuration for a project (one entry per environment).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EnvironmentConfigurationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfigurations(string code, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new GetProjectConfigurationsQuery(code), cancellationToken));

    /// <summary>Save one environment's configuration (upsert + replace headers).</summary>
    /// <remarks>Secret header values are encrypted at rest; send <c>valueProvided=false</c> to keep a stored secret.</remarks>
    [HttpPut("{environment}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveConfiguration(
        string code,
        string environment,
        [FromBody] SaveEnvironmentConfigurationRequest body,
        CancellationToken cancellationToken
    )
    {
        var headers = (body.Headers ?? [])
            .Select(header => new ConfigurationHeaderInput(header.Key, header.Value, header.IsSecret, header.ValueProvided))
            .ToList();

        return HandleResult(
            await sender.Send(
                new SaveEnvironmentConfigurationCommand(code, environment, body.BaseUrl, headers),
                cancellationToken
            )
        );
    }
}
