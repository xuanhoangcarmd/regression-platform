using MediatR;
using RegressionPlatform.Application.Features.DataLibrary.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.DataLibrary.Queries.GetYmmeOptions;

/// <summary>Distinct YMME options (faceted by the given selections) for the cascading filter.</summary>
public sealed record GetYmmeOptionsQuery(
    string? Year = null,
    string? Make = null,
    string? Model = null,
    string? Engine = null
) : IRequest<Result<YmmeOptionsResponse>>;
