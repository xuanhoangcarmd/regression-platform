using MediatR;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.ProjectConfiguration.Contracts;
using RegressionPlatform.Shared.Results;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.ProjectConfiguration.Commands.SaveEnvironmentConfiguration;

public sealed class SaveEnvironmentConfigurationCommandHandler(
    IProjectConfigurationRepository repository,
    ISecretProtector secretProtector
) : IRequestHandler<SaveEnvironmentConfigurationCommand, Result>
{
    public async Task<Result> Handle(SaveEnvironmentConfigurationCommand request, CancellationToken cancellationToken)
    {
        var projectId = await repository.GetProjectIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result.NotFound(new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist."));
        }

        var environment = request.Environment.ToUpperInvariant();
        if (!ConfigurationEnvironments.All.Contains(environment))
        {
            return Result.Failure(new Error("Configuration.InvalidEnvironment", $"Environment '{environment}' is invalid."));
        }

        // Existing headers (keyed by name) let us preserve unchanged secrets without re-sending them.
        var existing = await repository.GetConfigurationsAsync(projectId.Value, cancellationToken);
        var existingHeaders =
            existing
                .FirstOrDefault(configuration =>
                    string.Equals(configuration.Environment, environment, StringComparison.OrdinalIgnoreCase)
                )
                ?.Headers ?? [];
        var existingByKey = existingHeaders
            .GroupBy(header => header.HeaderKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

        // Dedup incoming headers by key (last wins) so the unique (ConfigurationId, HeaderKey) holds.
        var deduped = request
            .Headers.Where(header => !string.IsNullOrWhiteSpace(header.Key))
            .GroupBy(header => header.Key.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last());

        var toPersist = new List<DomainEntities.ProjectConfigurationHeader>();
        foreach (var input in deduped)
        {
            var header = new DomainEntities.ProjectConfigurationHeader
            {
                HeaderKey = input.Key.Trim(),
                IsSecret = input.IsSecret,
            };

            if (input.IsSecret)
            {
                if (input.ValueProvided)
                {
                    // New secret entered — encrypt it (empty value clears it).
                    header.SecretValueEncrypted = string.IsNullOrEmpty(input.Value)
                        ? null
                        : secretProtector.Encrypt(input.Value);
                }
                else
                {
                    // Unchanged (masked) secret — keep the stored ciphertext.
                    header.SecretValueEncrypted = existingByKey.TryGetValue(input.Key.Trim(), out var previous)
                        ? previous.SecretValueEncrypted
                        : null;
                }
            }
            else
            {
                header.HeaderValue = input.Value;
            }

            toPersist.Add(header);
        }

        await repository.SaveAsync(projectId.Value, environment, request.BaseUrl, toPersist, cancellationToken);
        return Result.Success();
    }
}
