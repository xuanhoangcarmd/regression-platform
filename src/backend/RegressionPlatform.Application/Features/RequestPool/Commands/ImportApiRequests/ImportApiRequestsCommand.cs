using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.RequestPool.Commands.ImportApiRequests;

/// <summary>
/// Bulk-imports requests for one API into a dataset from an uploaded Excel file.
/// <paramref name="File"/> is the open .xlsx stream (owned by the caller).
/// </summary>
public sealed record ImportApiRequestsCommand(int DatasetId, int ApiId, Stream File)
    : IRequest<Result<ImportApiRequestsResponse>>;
