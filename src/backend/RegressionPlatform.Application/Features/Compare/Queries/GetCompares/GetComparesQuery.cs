using MediatR;
using RegressionPlatform.Application.Features.Compare.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Queries.GetCompares;

/// <summary>Lists a project's compare jobs (newest first) for the Compare History screen.</summary>
public sealed record GetComparesQuery(string ProjectCode) : IRequest<Result<IReadOnlyList<CompareJobListItem>>>;
