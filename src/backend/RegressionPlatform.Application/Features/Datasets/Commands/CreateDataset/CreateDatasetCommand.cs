using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Datasets.Commands.CreateDataset;

/// <summary>Creates a new dataset in a project. Starts Active.</summary>
public sealed record CreateDatasetCommand(string ProjectCode, string Name, string? Description) : IRequest<Result<int>>;
