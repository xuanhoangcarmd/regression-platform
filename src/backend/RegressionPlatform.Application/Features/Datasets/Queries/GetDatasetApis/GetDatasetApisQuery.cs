using MediatR;
using RegressionPlatform.Application.Features.Datasets.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Datasets.Queries.GetDatasetApis;

/// <summary>Lists the APIs that have requests in a dataset (for the run-create API picker).</summary>
public sealed record GetDatasetApisQuery(int DatasetId) : IRequest<Result<IReadOnlyList<DatasetApiResponse>>>;
