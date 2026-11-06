using MediatR;
using Microsoft.AspNetCore.Mvc;
using RegressionPlatform.Application.Features.Projects.Contracts;
using RegressionPlatform.Application.Features.Projects.Queries.GetProjects;

namespace RegressionPlatform.API.Controllers;

/// <summary>Projects (for navigation / project switching).</summary>
[Route("api/projects")]
public sealed class ProjectsController(ISender sender) : ApiControllerBase
{
    /// <summary>List all active projects.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjects(CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new GetProjectsQuery(), cancellationToken));
}
