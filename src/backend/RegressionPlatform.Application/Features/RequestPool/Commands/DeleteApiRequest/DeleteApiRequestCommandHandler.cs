using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.RequestPool.Commands.DeleteApiRequest;

public sealed class DeleteApiRequestCommandHandler(IApiRequestRepository repository)
    : IRequestHandler<DeleteApiRequestCommand, Result>
{
    public async Task<Result> Handle(DeleteApiRequestCommand request, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(request.Id, cancellationToken);

        return deleted
            ? Result.Success()
            : Result.NotFound(new Error("Request.NotFound", $"Request {request.Id} was not found."));
    }
}
