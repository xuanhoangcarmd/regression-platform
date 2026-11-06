using MediatR;
using RegressionPlatform.Application.Features.IgnoreRule.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.IgnoreRule.Queries.GetIgnoreRules;

/// <summary>Lists a project's compare ignore rules.</summary>
public sealed record GetIgnoreRulesQuery(string ProjectCode) : IRequest<Result<IReadOnlyList<IgnoreRuleResponse>>>;
