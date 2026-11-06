using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Compare.Commands.DeleteCompare;

/// <summary>Deletes a compare and ALL data derived from it (result, per-request details, API links).</summary>
public sealed record DeleteCompareCommand(string ProjectCode, long CompareId) : IRequest<Result>;
