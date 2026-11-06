using MediatR;
using RegressionPlatform.Application.Features.ProjectConfiguration.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.ProjectConfiguration.Queries.GetProjectConfigurations;

/// <summary>Gets every environment's configuration for a project (by route code).</summary>
public sealed record GetProjectConfigurationsQuery(string ProjectCode)
    : IRequest<Result<IReadOnlyList<EnvironmentConfigurationResponse>>>;
