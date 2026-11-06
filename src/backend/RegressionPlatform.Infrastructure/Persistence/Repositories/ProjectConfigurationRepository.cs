using Dapper;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.ProjectConfiguration;
using RegressionPlatform.Infrastructure.Persistence.Sql;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Infrastructure.Persistence.Repositories;

/// <summary>Dapper data access for project environment configuration.</summary>
public sealed class ProjectConfigurationRepository(IDbConnectionFactory connectionFactory)
    : IProjectConfigurationRepository
{
    public async Task<int?> GetProjectIdByCodeAsync(string code, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(ProjectConfigurationSql.GetProjectIdByCode, new { Code = code }, cancellationToken: cancellationToken)
        );
    }

    public async Task<IReadOnlyList<DomainEntities.ProjectConfiguration>> GetConfigurationsAsync(
        int projectId,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(ProjectConfigurationSql.GetByProject, new { ProjectId = projectId }, cancellationToken: cancellationToken)
        );

        var configurations = (await multi.ReadAsync<DomainEntities.ProjectConfiguration>()).ToList();
        var headers = (await multi.ReadAsync<DomainEntities.ProjectConfigurationHeader>()).ToList();

        var headersByConfiguration = headers
            .GroupBy(header => header.ConfigurationId)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var configuration in configurations)
        {
            if (headersByConfiguration.TryGetValue(configuration.Id, out var configurationHeaders))
            {
                configuration.Headers = configurationHeaders;
            }
        }

        return configurations;
    }

    public async Task SaveAsync(
        int projectId,
        string environment,
        string? baseUrl,
        IReadOnlyList<DomainEntities.ProjectConfigurationHeader> headers,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        // Upsert the (project, environment) configuration row.
        var configurationId = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                ProjectConfigurationSql.GetConfigurationId,
                new { ProjectId = projectId, Environment = environment },
                transaction,
                cancellationToken: cancellationToken
            )
        );

        if (configurationId is null)
        {
            configurationId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    ProjectConfigurationSql.InsertConfiguration,
                    new { ProjectId = projectId, Environment = environment, BaseUrl = baseUrl },
                    transaction,
                    cancellationToken: cancellationToken
                )
            );
        }
        else
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    ProjectConfigurationSql.UpdateConfiguration,
                    new { Id = configurationId.Value, BaseUrl = baseUrl },
                    transaction,
                    cancellationToken: cancellationToken
                )
            );
        }

        // Replace all headers for this configuration.
        await connection.ExecuteAsync(
            new CommandDefinition(
                ProjectConfigurationSql.DeleteHeaders,
                new { ConfigurationId = configurationId.Value },
                transaction,
                cancellationToken: cancellationToken
            )
        );

        if (headers.Count > 0)
        {
            var rows = headers.Select(header => new
            {
                ConfigurationId = configurationId.Value,
                header.HeaderKey,
                header.HeaderValue,
                header.SecretValueEncrypted,
                header.IsSecret,
            });

            await connection.ExecuteAsync(
                new CommandDefinition(ProjectConfigurationSql.InsertHeader, rows, transaction, cancellationToken: cancellationToken)
            );
        }

        transaction.Commit();
    }
}
