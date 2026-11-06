using Dapper;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Projects.Contracts;

namespace RegressionPlatform.Infrastructure.Persistence.Repositories;

/// <summary>Dapper data access for projects.</summary>
public sealed class ProjectRepository(IDbConnectionFactory connectionFactory) : IProjectRepository
{
    private const string GetAllSql = "SELECT Id, Code, Name FROM Project WHERE IsActive = 1 ORDER BY Id;";

    private const string GetIdByCodeSql = "SELECT Id FROM Project WHERE Code = @Code AND IsActive = 1;";

    public async Task<IReadOnlyList<ProjectResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var projects = await connection.QueryAsync<ProjectResponse>(
            new CommandDefinition(GetAllSql, cancellationToken: cancellationToken)
        );
        return projects.ToList();
    }

    public async Task<int?> GetIdByCodeAsync(string code, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(GetIdByCodeSql, new { Code = code }, cancellationToken: cancellationToken)
        );
    }
}
