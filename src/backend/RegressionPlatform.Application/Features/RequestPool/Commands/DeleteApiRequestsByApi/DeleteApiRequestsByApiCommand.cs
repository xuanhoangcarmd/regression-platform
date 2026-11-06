using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.RequestPool.Commands.DeleteApiRequestsByApi;

/// <summary>Deletes ALL requests of an API within a dataset (plus their captured responses). Returns the count.</summary>
public sealed record DeleteApiRequestsByApiCommand(int DatasetId, int ApiId) : IRequest<Result<int>>;
