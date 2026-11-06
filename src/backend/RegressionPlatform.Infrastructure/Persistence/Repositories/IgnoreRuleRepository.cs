using Dapper;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.IgnoreRule;
using RegressionPlatform.Application.Features.IgnoreRule.Contracts;
using RegressionPlatform.Infrastructure.Persistence.Sql;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Infrastructure.Persistence.Repositories;

/// <summary>Dapper data access for compare ignore rules and the APIs they apply to.</summary>
public sealed class IgnoreRuleRepository(IDbConnectionFactory connectionFactory) : IIgnoreRuleRepository
{
    private sealed record RuleRow(int Id, string JsonPath, string? Description, bool Enabled, string RuleType, string? Delimiter);

    private sealed record LinkRow(int IgnoreRuleId, int ApiId, string ApiName);

    public async Task<IReadOnlyList<IgnoreRuleResponse>> GetAllAsync(int projectId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(IgnoreRuleSql.GetAll, new { ProjectId = projectId }, cancellationToken: cancellationToken)
        );

        var rules = (await multi.ReadAsync<RuleRow>()).ToList();
        var links = (await multi.ReadAsync<LinkRow>()).ToList();
        var apisByRule = LinksByRule(links);

        return rules.Select(rule => ToResponse(rule, apisByRule)).ToList();
    }

    public async Task<IgnoreRuleResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(IgnoreRuleSql.GetById, new { Id = id }, cancellationToken: cancellationToken)
        );

        var rule = await multi.ReadSingleOrDefaultAsync<RuleRow>();
        if (rule is null)
        {
            return null;
        }

        var links = (await multi.ReadAsync<LinkRow>()).ToList();
        return ToResponse(rule, LinksByRule(links));
    }

    public async Task<int> CreateAsync(
        DomainEntities.CompareIgnoreRule rule,
        IReadOnlyList<int> apiIds,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var id = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                IgnoreRuleSql.Insert,
                new { rule.ProjectId, rule.JsonPath, rule.Description, rule.Enabled, rule.RuleType, rule.Delimiter },
                transaction,
                cancellationToken: cancellationToken
            )
        );

        await InsertLinksAsync(connection, transaction, id, apiIds, cancellationToken);

        transaction.Commit();
        return id;
    }

    public async Task<bool> UpdateAsync(
        DomainEntities.CompareIgnoreRule rule,
        IReadOnlyList<int> apiIds,
        CancellationToken cancellationToken
    )
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                IgnoreRuleSql.Update,
                new { rule.Id, rule.JsonPath, rule.Description, rule.Enabled, rule.RuleType, rule.Delimiter },
                transaction,
                cancellationToken: cancellationToken
            )
        );

        await connection.ExecuteAsync(
            new CommandDefinition(
                IgnoreRuleSql.DeleteLinks,
                new { IgnoreRuleId = rule.Id },
                transaction,
                cancellationToken: cancellationToken
            )
        );
        await InsertLinksAsync(connection, transaction, rule.Id, apiIds, cancellationToken);

        transaction.Commit();
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(IgnoreRuleSql.Delete, new { Id = id }, cancellationToken: cancellationToken)
        );
        return rows > 0;
    }

    private static async Task InsertLinksAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        int ruleId,
        IReadOnlyList<int> apiIds,
        CancellationToken cancellationToken
    )
    {
        var distinctIds = apiIds.Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return;
        }

        var rows = distinctIds.Select(apiId => new { IgnoreRuleId = ruleId, ApiId = apiId });
        await connection.ExecuteAsync(
            new CommandDefinition(IgnoreRuleSql.InsertLink, rows, transaction, cancellationToken: cancellationToken)
        );
    }

    private static IReadOnlyDictionary<int, List<IgnoreRuleApiRef>> LinksByRule(IEnumerable<LinkRow> links) =>
        links
            .GroupBy(link => link.IgnoreRuleId)
            .ToDictionary(group => group.Key, group => group.Select(l => new IgnoreRuleApiRef(l.ApiId, l.ApiName)).ToList());

    private static IgnoreRuleResponse ToResponse(
        RuleRow rule,
        IReadOnlyDictionary<int, List<IgnoreRuleApiRef>> apisByRule
    ) =>
        new(
            rule.Id,
            rule.JsonPath,
            rule.Description,
            rule.Enabled,
            rule.RuleType,
            rule.Delimiter,
            apisByRule.TryGetValue(rule.Id, out var apis) ? apis : []
        );
}
