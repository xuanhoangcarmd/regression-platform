using MediatR;
using RegressionPlatform.Application.Features.Projects.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Projects.Queries.GetProjects;

/// <summary>Lists all active projects.</summary>
public sealed record GetProjectsQuery : IRequest<Result<IReadOnlyList<ProjectResponse>>>;
