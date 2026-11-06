using MediatR;
using RegressionPlatform.Application.Features.DataLibrary.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.DataLibrary.Queries.GetRawBufferContent;

/// <summary>Gets one raw buffer's decompressed content as base64.</summary>
public sealed record GetRawBufferContentQuery(long Id) : IRequest<Result<RawBufferContentResponse>>;
