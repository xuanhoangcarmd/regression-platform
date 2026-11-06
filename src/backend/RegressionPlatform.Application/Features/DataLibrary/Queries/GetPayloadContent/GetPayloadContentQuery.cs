using MediatR;
using RegressionPlatform.Application.Features.DataLibrary.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.DataLibrary.Queries.GetPayloadContent;

/// <summary>Gets one payload's decompressed content as base64.</summary>
public sealed record GetPayloadContentQuery(long Id) : IRequest<Result<PayloadContentResponse>>;
