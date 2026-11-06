using MediatR;
using RegressionPlatform.Application.Features.ApiDefinition.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.ApiDefinition.Queries.GetApiById;

public sealed class GetApiByIdQueryHandler(IApiRepository repository)
    : IRequestHandler<GetApiByIdQuery, Result<ApiResponse>>
{
    public async Task<Result<ApiResponse>> Handle(GetApiByIdQuery request, CancellationToken cancellationToken)
    {
        var api = await repository.GetByIdAsync(request.Id, cancellationToken);

        return api is null
            ? Result<ApiResponse>.NotFound(new Error("Api.NotFound", $"API {request.Id} was not found."))
            : Result<ApiResponse>.Success(api);
    }
}
