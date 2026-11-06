using MediatR;
using RegressionPlatform.Application.Features.DataLibrary.Contracts;
using RegressionPlatform.Shared.Pagination;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.DataLibrary.Queries.SearchVehicleProfiles;

public sealed class SearchVehicleProfilesQueryHandler(IDataLibraryRepository repository)
    : IRequestHandler<SearchVehicleProfilesQuery, Result<PagedResult<VehicleProfileListItem>>>
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<PagedResult<VehicleProfileListItem>>> Handle(
        SearchVehicleProfilesQuery request,
        CancellationToken cancellationToken
    )
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);

        var parameters = new VehicleProfileSearchParameters(
            request.Vin,
            request.Year,
            request.Make,
            request.Model,
            request.Engine,
            request.ToolMilStatus,
            request.ToolLedStatus,
            request.PrimaryDtc,
            request.DtcCode,
            request.DtcSource,
            request.DtcSystem,
            request.DtcStatusType,
            request.DtcDefinition,
            request.DtcFormat,
            request.DtcPrefix,
            request.PrimaryDtcMode,
            request.DtcCodeMode,
            request.HasRawBuffer,
            request.YearFrom,
            request.YearTo,
            request.SortBy,
            request.SortDirection,
            pageNumber,
            pageSize
        );

        var page = await repository.SearchProfilesAsync(parameters, cancellationToken);
        return Result<PagedResult<VehicleProfileListItem>>.Success(page);
    }
}
