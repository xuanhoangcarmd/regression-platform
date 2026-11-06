using MediatR;
using RegressionPlatform.Application.Features.DataLibrary.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.DataLibrary.Queries.GetVehicleProfileById;

public sealed class GetVehicleProfileByIdQueryHandler(IDataLibraryRepository repository)
    : IRequestHandler<GetVehicleProfileByIdQuery, Result<VehicleProfileDetailResponse>>
{
    public async Task<Result<VehicleProfileDetailResponse>> Handle(
        GetVehicleProfileByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var profile = await repository.GetProfileByIdAsync(request.Id, cancellationToken);
        return profile is null
            ? Result<VehicleProfileDetailResponse>.NotFound(
                new Error("VehicleProfile.NotFound", $"Vehicle profile {request.Id} was not found.")
            )
            : Result<VehicleProfileDetailResponse>.Success(profile);
    }
}
