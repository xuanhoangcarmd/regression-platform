using MediatR;
using RegressionPlatform.Application.Features.DataLibrary.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.DataLibrary.Queries.GetRawBufferContent;

public sealed class GetRawBufferContentQueryHandler(IDataLibraryRepository repository)
    : IRequestHandler<GetRawBufferContentQuery, Result<RawBufferContentResponse>>
{
    public async Task<Result<RawBufferContentResponse>> Handle(GetRawBufferContentQuery request, CancellationToken cancellationToken)
    {
        var content = await repository.GetRawBufferContentAsync(request.Id, cancellationToken);
        return content is null
            ? Result<RawBufferContentResponse>.NotFound(new Error("RawBuffer.NotFound", $"Raw buffer {request.Id} was not found."))
            : Result<RawBufferContentResponse>.Success(content);
    }
}
