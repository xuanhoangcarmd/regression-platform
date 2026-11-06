using MediatR;
using RegressionPlatform.Application.Features.DataLibrary.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.DataLibrary.Queries.GetPayloadContent;

public sealed class GetPayloadContentQueryHandler(IDataLibraryRepository repository)
    : IRequestHandler<GetPayloadContentQuery, Result<PayloadContentResponse>>
{
    public async Task<Result<PayloadContentResponse>> Handle(GetPayloadContentQuery request, CancellationToken cancellationToken)
    {
        var content = await repository.GetPayloadContentAsync(request.Id, cancellationToken);
        return content is null
            ? Result<PayloadContentResponse>.NotFound(new Error("Payload.NotFound", $"Payload {request.Id} was not found."))
            : Result<PayloadContentResponse>.Success(content);
    }
}
