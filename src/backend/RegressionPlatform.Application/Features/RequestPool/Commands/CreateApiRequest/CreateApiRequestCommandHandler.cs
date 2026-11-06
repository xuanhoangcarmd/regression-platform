using MediatR;
using RegressionPlatform.Domain.Entities;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.RequestPool.Commands.CreateApiRequest;

public sealed class CreateApiRequestCommandHandler(IApiRequestRepository repository)
    : IRequestHandler<CreateApiRequestCommand, Result<long>>
{
    public async Task<Result<long>> Handle(CreateApiRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = new ApiRequest
        {
            DatasetId = request.DatasetId,
            ApiId = request.ApiId,
            Title = request.Title,
            Description = request.Description,
            HeadersJson = request.HeadersJson,
            QueryStringJson = request.QueryStringJson,
            RequestBody = request.RequestBody,
            Version = 1,
            IsActive = true,
        };

        var id = await repository.CreateAsync(entity, cancellationToken);
        return Result<long>.Success(id);
    }
}
