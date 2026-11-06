using MediatR;
using RegressionPlatform.Application.Features.IgnoreRule.Contracts;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.IgnoreRule.Queries.GetIgnoreRules;

public sealed class GetIgnoreRulesQueryHandler(IIgnoreRuleRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<GetIgnoreRulesQuery, Result<IReadOnlyList<IgnoreRuleResponse>>>
{
    public async Task<Result<IReadOnlyList<IgnoreRuleResponse>>> Handle(
        GetIgnoreRulesQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<IReadOnlyList<IgnoreRuleResponse>>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var rules = await repository.GetAllAsync(projectId.Value, cancellationToken);
        return Result<IReadOnlyList<IgnoreRuleResponse>>.Success(rules);
    }
}
