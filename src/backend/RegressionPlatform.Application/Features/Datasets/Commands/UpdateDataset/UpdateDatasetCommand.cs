using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Datasets.Commands.UpdateDataset;

/// <summary>Renames a dataset and/or toggles its active state.</summary>
public sealed record UpdateDatasetCommand(int Id, string Name, string? Description, bool IsActive) : IRequest<Result>;
