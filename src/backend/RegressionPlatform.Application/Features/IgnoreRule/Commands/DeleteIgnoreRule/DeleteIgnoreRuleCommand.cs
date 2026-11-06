using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.IgnoreRule.Commands.DeleteIgnoreRule;

/// <summary>Deletes a compare ignore rule.</summary>
public sealed record DeleteIgnoreRuleCommand(int Id) : IRequest<Result>;
