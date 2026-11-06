using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.IgnoreRule.Commands.DeleteIgnoreRule;

public sealed class DeleteIgnoreRuleCommandHandler(IIgnoreRuleRepository repository)
    : IRequestHandler<DeleteIgnoreRuleCommand, Result>
{
    public async Task<Result> Handle(DeleteIgnoreRuleCommand request, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(request.Id, cancellationToken);

        return deleted
            ? Result.Success()
            : Result.NotFound(new Error("IgnoreRule.NotFound", $"Ignore rule {request.Id} was not found."));
    }
}
