using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.IgnoreRule.Commands.UpdateIgnoreRule;

/// <summary>Updates an ignore rule (id from the route). Empty <paramref name="ApiIds"/> = applies to all APIs.</summary>
public sealed record UpdateIgnoreRuleCommand(
    int Id,
    string JsonPath,
    string? Description,
    bool Enabled,
    string RuleType,
    IReadOnlyList<int> ApiIds,
    /// <summary>TrimAfter only: compare the value only up to this text (e.g. "?").</summary>
    string? Delimiter = null
) : IRequest<Result>;
