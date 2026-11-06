using MediatR;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Queries.GetRunExport;

/// <summary>Every executed request of a run (API, input, full response, timing) — for the Excel export.</summary>
public sealed record GetRunExportQuery(string ProjectCode, long RunId)
    : IRequest<Result<IReadOnlyList<RunResponseExportRow>>>;
