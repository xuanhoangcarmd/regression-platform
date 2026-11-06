using MediatR;
using RegressionPlatform.Application.Features.Datasets.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Datasets.Queries.GetDatasets;

/// <summary>Lists a project's datasets (with request + API counts) for the Dataset List screen.</summary>
public sealed record GetDatasetsQuery(string ProjectCode) : IRequest<Result<IReadOnlyList<DatasetListItemResponse>>>;
