using MediatR;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Queries.GetRuns;

/// <summary>Lists a project's regression runs (newest first).</summary>
public sealed record GetRunsQuery(string ProjectCode) : IRequest<Result<IReadOnlyList<RunResponse>>>;
