using MediatR;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.ExpectedResponses.GetExpectedApis;

/// <summary>APIs of a run with request + edited (expected) counts — the editor's API picker.</summary>
public sealed record GetExpectedApisQuery(string ProjectCode, long RunId) : IRequest<Result<IReadOnlyList<ExpectedApiItem>>>;
