using MediatR;
using RegressionPlatform.Application.Features.IgnoreRule.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.IgnoreRule.Commands.CreateIgnoreRule;

public sealed class CreateIgnoreRuleCommandHandler(IIgnoreRuleRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<CreateIgnoreRuleCommand, Result<IgnoreRuleResponse>>
{
    public async Task<Result<IgnoreRuleResponse>> Handle(
        CreateIgnoreRuleCommand request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<IgnoreRuleResponse>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var rule = new DomainEntities.CompareIgnoreRule
        {
            ProjectId = projectId.Value,
            JsonPath = request.JsonPath,
            Description = request.Description,
            Enabled = request.Enabled,
            RuleType = request.RuleType,
            // Only TrimAfter uses a delimiter; never keep a stale one on the other kinds.
            Delimiter = request.RuleType == DomainEntities.CompareIgnoreRuleTypes.TrimAfter ? request.Delimiter : null,
        };

        var id = await repository.CreateAsync(rule, request.ApiIds, cancellationToken);

        // Re-read so the response includes the linked APIs (with names).
        var created = await repository.GetByIdAsync(id, cancellationToken);
        return created is null
            ? Result<IgnoreRuleResponse>.Failure(new Error("IgnoreRule.CreateFailed", "Failed to create the rule."))
            : Result<IgnoreRuleResponse>.Success(created);
    }
}
