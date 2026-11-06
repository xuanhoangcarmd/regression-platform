using MediatR;
using RegressionPlatform.Shared.Results;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.IgnoreRule.Commands.UpdateIgnoreRule;

public sealed class UpdateIgnoreRuleCommandHandler(IIgnoreRuleRepository repository)
    : IRequestHandler<UpdateIgnoreRuleCommand, Result>
{
    public async Task<Result> Handle(UpdateIgnoreRuleCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            return Result.NotFound(new Error("IgnoreRule.NotFound", $"Ignore rule {request.Id} was not found."));
        }

        var rule = new DomainEntities.CompareIgnoreRule
        {
            Id = request.Id,
            JsonPath = request.JsonPath,
            Description = request.Description,
            Enabled = request.Enabled,
            RuleType = request.RuleType,
            // Only TrimAfter uses a delimiter; never keep a stale one on the other kinds.
            Delimiter = request.RuleType == DomainEntities.CompareIgnoreRuleTypes.TrimAfter ? request.Delimiter : null,
        };

        await repository.UpdateAsync(rule, request.ApiIds, cancellationToken);
        return Result.Success();
    }
}
