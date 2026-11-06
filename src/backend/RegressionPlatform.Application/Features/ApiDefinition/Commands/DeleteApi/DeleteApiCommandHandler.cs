using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.ApiDefinition.Commands.DeleteApi;

public sealed class DeleteApiCommandHandler(IApiRepository repository) : IRequestHandler<DeleteApiCommand, Result>
{
    public async Task<Result> Handle(DeleteApiCommand request, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(request.Id, cancellationToken);

        return deleted
            ? Result.Success()
            : Result.NotFound(new Error("Api.NotFound", $"API {request.Id} was not found."));
    }
}
