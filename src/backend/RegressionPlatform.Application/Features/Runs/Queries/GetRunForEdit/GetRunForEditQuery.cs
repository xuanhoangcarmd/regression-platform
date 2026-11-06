using MediatR;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Queries.GetRunForEdit;

/// <summary>Editable fields of a run (to prefill the edit form), scoped to the project.</summary>
public sealed record GetRunForEditQuery(string ProjectCode, long RunId) : IRequest<Result<RunEditResponse>>;
