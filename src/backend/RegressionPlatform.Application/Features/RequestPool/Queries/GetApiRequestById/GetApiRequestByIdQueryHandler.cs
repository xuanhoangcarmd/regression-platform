using MediatR;
using RegressionPlatform.Application.Features.RequestPool.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.RequestPool.Queries.GetApiRequestById;

public sealed class GetApiRequestByIdQueryHandler(IApiRequestRepository repository)
    : IRequestHandler<GetApiRequestByIdQuery, Result<ApiRequestDetailResponse>>
{
    public async Task<Result<ApiRequestDetailResponse>> Handle(
        GetApiRequestByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var detail = await repository.GetByIdAsync(request.Id, cancellationToken);

        return detail is null
            ? Result<ApiRequestDetailResponse>.NotFound(new Error("Request.NotFound", $"Request {request.Id} was not found."))
            : Result<ApiRequestDetailResponse>.Success(detail);
    }
}
