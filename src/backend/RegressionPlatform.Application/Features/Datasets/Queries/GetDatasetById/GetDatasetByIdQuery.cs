using MediatR;
using RegressionPlatform.Application.Features.Datasets.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Datasets.Queries.GetDatasetById;

/// <summary>Gets one dataset (detail header / edit prefill).</summary>
public sealed record GetDatasetByIdQuery(int Id) : IRequest<Result<DatasetResponse>>;
