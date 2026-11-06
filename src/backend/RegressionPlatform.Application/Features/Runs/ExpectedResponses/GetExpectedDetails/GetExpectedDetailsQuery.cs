using MediatR;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.ExpectedResponses.GetExpectedDetails;

/// <summary>Original + edited bodies for several requests of a run (editor + bulk-edit preview).</summary>
public sealed record GetExpectedDetailsQuery(string ProjectCode, long RunId, IReadOnlyList<long> RequestIds)
    : IRequest<Result<IReadOnlyList<ExpectedResponseDetail>>>;
