using MediatR;
using RegressionPlatform.Application.Features.DataLibrary.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.DataLibrary.Queries.GetYmmeOptions;

public sealed class GetYmmeOptionsQueryHandler(IDataLibraryRepository repository)
    : IRequestHandler<GetYmmeOptionsQuery, Result<YmmeOptionsResponse>>
{
    public async Task<Result<YmmeOptionsResponse>> Handle(GetYmmeOptionsQuery request, CancellationToken cancellationToken)
    {
        var options = await repository.GetYmmeOptionsAsync(
            request.Year, request.Make, request.Model, request.Engine, cancellationToken
        );
        return Result<YmmeOptionsResponse>.Success(options);
    }
}
