using RegressionPlatform.Application.Features.IgnoreRule.Contracts;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.IgnoreRule;

/// <summary>Data access for compare ignore rules (and the APIs they apply to). Implemented in Infrastructure.</summary>
public interface IIgnoreRuleRepository
{
    Task<IReadOnlyList<IgnoreRuleResponse>> GetAllAsync(int projectId, CancellationToken cancellationToken);

    Task<IgnoreRuleResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Creates a rule and its API links (empty <paramref name="apiIds"/> = all APIs). Returns the new id.</summary>
    Task<int> CreateAsync(DomainEntities.CompareIgnoreRule rule, IReadOnlyList<int> apiIds, CancellationToken cancellationToken);

    /// <summary>Updates a rule and replaces its API links. Returns true if the rule existed.</summary>
    Task<bool> UpdateAsync(DomainEntities.CompareIgnoreRule rule, IReadOnlyList<int> apiIds, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
