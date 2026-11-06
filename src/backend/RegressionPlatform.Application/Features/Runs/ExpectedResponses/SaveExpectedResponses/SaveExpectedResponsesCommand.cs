using MediatR;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.ExpectedResponses.SaveExpectedResponses;

/// <summary>
/// Saves (upserts) edited expected responses of a run in one transaction. Returns how many were saved.
/// The original captured RunResponse rows are never modified.
/// </summary>
public sealed record SaveExpectedResponsesCommand(string ProjectCode, long RunId, IReadOnlyList<ExpectedResponseSaveItem> Items)
    : IRequest<Result<int>>;
