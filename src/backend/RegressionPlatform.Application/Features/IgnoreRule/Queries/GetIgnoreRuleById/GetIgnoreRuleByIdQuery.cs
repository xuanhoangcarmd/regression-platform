using MediatR;
using RegressionPlatform.Application.Features.IgnoreRule.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.IgnoreRule.Queries.GetIgnoreRuleById;

/// <summary>Gets a single ignore rule by id.</summary>
public sealed record GetIgnoreRuleByIdQuery(int Id) : IRequest<Result<IgnoreRuleResponse>>;
