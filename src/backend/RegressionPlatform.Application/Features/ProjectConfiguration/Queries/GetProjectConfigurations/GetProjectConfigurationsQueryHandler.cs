using MediatR;
using RegressionPlatform.Application.Features.ProjectConfiguration.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.ProjectConfiguration.Queries.GetProjectConfigurations;

public sealed class GetProjectConfigurationsQueryHandler(IProjectConfigurationRepository repository)
    : IRequestHandler<GetProjectConfigurationsQuery, Result<IReadOnlyList<EnvironmentConfigurationResponse>>>
{
    public async Task<Result<IReadOnlyList<EnvironmentConfigurationResponse>>> Handle(
        GetProjectConfigurationsQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await repository.GetProjectIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<IReadOnlyList<EnvironmentConfigurationResponse>>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var configurations = await repository.GetConfigurationsAsync(projectId.Value, cancellationToken);
        var byEnvironment = configurations.ToDictionary(
            configuration => configuration.Environment,
            StringComparer.OrdinalIgnoreCase
        );

        // Always return one entry per supported environment (empty when not configured yet).
        var responses = ConfigurationEnvironments
            .All.Select(environment =>
            {
                if (!byEnvironment.TryGetValue(environment, out var configuration))
                {
                    return new EnvironmentConfigurationResponse(environment, null, []);
                }

                var headers = configuration
                    .Headers.Select(header => new ConfigurationHeaderResponse(
                        header.HeaderKey,
                        // Secret values are never returned; non-secret values are returned as-is.
                        header.IsSecret ? null : header.HeaderValue,
                        header.IsSecret,
                        header.IsSecret
                            ? header.SecretValueEncrypted is { Length: > 0 }
                            : !string.IsNullOrEmpty(header.HeaderValue)
                    ))
                    .ToList();

                return new EnvironmentConfigurationResponse(environment, configuration.BaseUrl, headers);
            })
            .ToList();

        return Result<IReadOnlyList<EnvironmentConfigurationResponse>>.Success(responses);
    }
}
