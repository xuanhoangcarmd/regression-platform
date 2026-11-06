using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.ProjectConfiguration;

/// <summary>Data access for project environment configuration. Implemented in Infrastructure.</summary>
public interface IProjectConfigurationRepository
{
    /// <summary>Resolves an active project's id by its route code, or null if not found.</summary>
    Task<int?> GetProjectIdByCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>All configurations (with their headers) for a project, across environments.</summary>
    Task<IReadOnlyList<DomainEntities.ProjectConfiguration>> GetConfigurationsAsync(
        int projectId,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Upserts the configuration for (project, environment) and replaces its headers with
    /// <paramref name="headers"/>, all in one transaction.
    /// </summary>
    Task SaveAsync(
        int projectId,
        string environment,
        string? baseUrl,
        IReadOnlyList<DomainEntities.ProjectConfigurationHeader> headers,
        CancellationToken cancellationToken
    );
}
