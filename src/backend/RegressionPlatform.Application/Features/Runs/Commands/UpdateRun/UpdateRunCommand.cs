using MediatR;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Commands.UpdateRun;

/// <summary>
/// Updates a Pending run (never executed) in place. Empty <paramref name="ApiIds"/> = run all of the
/// dataset's APIs. Only Pending runs are editable.
/// </summary>
public sealed record UpdateRunCommand(
    string ProjectCode,
    long RunId,
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
