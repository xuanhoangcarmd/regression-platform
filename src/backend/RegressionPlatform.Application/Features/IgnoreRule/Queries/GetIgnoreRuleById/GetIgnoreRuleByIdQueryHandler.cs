using MediatR;
using RegressionPlatform.Application.Features.IgnoreRule.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.IgnoreRule.Queries.GetIgnoreRuleById;

public sealed class GetIgnoreRuleByIdQueryHandler(IIgnoreRuleRepository repository)
    : IRequestHandler<GetIgnoreRuleByIdQuery, Result<IgnoreRuleResponse>>
{
    public async Task<Result<IgnoreRuleResponse>> Handle(
        GetIgnoreRuleByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var rule = await repository.GetByIdAsync(request.Id, cancellationToken);

        return rule is null
            ? Result<IgnoreRuleResponse>.NotFound(new Error("IgnoreRule.NotFound", $"Ignore rule {request.Id} was not found."))
            : Result<IgnoreRuleResponse>.Success(rule);
    }
}
