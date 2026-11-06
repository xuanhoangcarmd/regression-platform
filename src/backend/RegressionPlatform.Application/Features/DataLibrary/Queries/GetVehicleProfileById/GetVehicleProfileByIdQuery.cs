using MediatR;
using RegressionPlatform.Application.Features.DataLibrary.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.DataLibrary.Queries.GetVehicleProfileById;

/// <summary>Gets full detail of one vehicle profile (snapshot + children + payload/buffer metadata).</summary>
public sealed record GetVehicleProfileByIdQuery(long Id) : IRequest<Result<VehicleProfileDetailResponse>>;
