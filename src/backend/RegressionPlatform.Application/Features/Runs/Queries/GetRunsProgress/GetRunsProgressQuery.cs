using MediatR;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Queries.GetRunsProgress;

/// <summary>
/// Live progress for a specific set of runs (the poll target for the Runs list). Callers pass only
/// the ids of runs currently visible and executing, so this stays cheap regardless of list size.
/// </summary>
public sealed record GetRunsProgressQuery(string ProjectCode, IReadOnlyList<long> RunIds)
    : IRequest<Result<IReadOnlyList<RunProgressResponse>>>;
