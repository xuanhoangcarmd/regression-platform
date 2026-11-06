using MediatR;
using RegressionPlatform.Domain.Entities;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.RequestPool.Commands.UpdateApiRequest;

public sealed class UpdateApiRequestCommandHandler(IApiRequestRepository repository)
    : IRequestHandler<UpdateApiRequestCommand, Result>
{
    public async Task<Result> Handle(UpdateApiRequestCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            return Result.NotFound(new Error("Request.NotFound", $"Request {request.Id} was not found."));
        }

        var entity = new ApiRequest
        {
            Id = request.Id,
            ApiId = request.ApiId,
            Title = request.Title,
            Description = request.Description,
            HeadersJson = request.HeadersJson,
            QueryStringJson = request.QueryStringJson,
            RequestBody = request.RequestBody,
        };

        await repository.UpdateAsync(entity, cancellationToken);
        return Result.Success();
    }
}
