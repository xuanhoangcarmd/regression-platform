using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.RequestPool.Commands.DeleteApiRequestsByApi;

public sealed class DeleteApiRequestsByApiCommandHandler(IApiRequestRepository repository)
    : IRequestHandler<DeleteApiRequestsByApiCommand, Result<int>>
{
    public async Task<Result<int>> Handle(DeleteApiRequestsByApiCommand request, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteByApiAsync(request.DatasetId, request.ApiId, cancellationToken);
        return Result<int>.Success(deleted);
    }
}
