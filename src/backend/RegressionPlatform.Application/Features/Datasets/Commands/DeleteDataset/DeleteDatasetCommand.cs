using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Datasets.Commands.DeleteDataset;

/// <summary>Deletes a dataset. Blocked while any request or run still references it.</summary>
public sealed record DeleteDatasetCommand(int Id) : IRequest<Result>;
