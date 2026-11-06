using MediatR;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Commands.CreateRun;

/// <summary>
/// Creates a new (pending) run in a project, executing one dataset.
/// Empty <paramref name="ApiIds"/> = run all of the dataset's APIs.
/// </summary>
public sealed record CreateRunCommand(
    string ProjectCode,
    int DatasetId,
    string Title,
    string? Description,
    string Environment,
    string RunType,
    int? RequestLimitPerApi,
    IReadOnlyList<int> ApiIds,
    int MaxParallelApis = 1,
    int MaxParallelRequestsPerApi = 1
) : IRequest<Result<RunResponse>>;
