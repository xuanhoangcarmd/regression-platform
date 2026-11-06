using MediatR;
using RegressionPlatform.Application.Features.IgnoreRule.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.IgnoreRule.Commands.CreateIgnoreRule;

/// <summary>Creates a compare ignore rule in a project. Empty <paramref name="ApiIds"/> = applies to all APIs.</summary>
public sealed record CreateIgnoreRuleCommand(
    string ProjectCode,
    string JsonPath,
    string? Description,
    bool Enabled,
    string RuleType,
    IReadOnlyList<int> ApiIds,
    /// <summary>TrimAfter only: compare the value only up to this text (e.g. "?").</summary>
    string? Delimiter = null
) : IRequest<Result<IgnoreRuleResponse>>;
