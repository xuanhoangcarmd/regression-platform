using MediatR;
using RegressionPlatform.Application.Features.DataLibrary.Contracts;
using RegressionPlatform.Shared.Pagination;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.DataLibrary.Queries.SearchVehicleProfiles;

/// <summary>Searches the Data Library by VIN / YMME / MIL / LED / DTC code.</summary>
public sealed record SearchVehicleProfilesQuery(
    string? Vin = null,
    string? Year = null,
    string? Make = null,
    string? Model = null,
    string? Engine = null,
    int? ToolMilStatus = null,
    int? ToolLedStatus = null,
    string? PrimaryDtc = null,
    string? DtcCode = null,
    string? DtcSource = null,
    string? DtcSystem = null,
    string? DtcStatusType = null,
    string? DtcDefinition = null,
    string? DtcFormat = null,
    string? DtcPrefix = null,
    string? PrimaryDtcMode = null,
    string? DtcCodeMode = null,
    bool? HasRawBuffer = null,
    int? YearFrom = null,
    int? YearTo = null,
    string? SortBy = null,
    string? SortDirection = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<VehicleProfileListItem>>>;
