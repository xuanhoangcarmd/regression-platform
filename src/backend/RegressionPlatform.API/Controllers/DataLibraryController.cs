using MediatR;
using Microsoft.AspNetCore.Mvc;
using RegressionPlatform.Application.Features.DataLibrary.Contracts;
using RegressionPlatform.Application.Features.DataLibrary.Queries.GetPayloadContent;
using RegressionPlatform.Application.Features.DataLibrary.Queries.GetRawBufferContent;
using RegressionPlatform.Application.Features.DataLibrary.Queries.GetVehicleProfileById;
using RegressionPlatform.Application.Features.DataLibrary.Queries.GetYmmeOptions;
using RegressionPlatform.Application.Features.DataLibrary.Queries.SearchVehicleProfiles;
using RegressionPlatform.Shared.Pagination;

namespace RegressionPlatform.API.Controllers;

/// <summary>The Data Library — a global catalog of vehicle payloads/raw buffers, searched by YMME / DTC / MIL.</summary>
[Route("api/data-library")]
public sealed class DataLibraryController(ISender sender) : ApiControllerBase
{
    /// <summary>Search vehicle profiles by VIN / YMME / MIL / DTC code (paged).</summary>
    [HttpGet("profiles")]
    [ProducesResponseType(typeof(PagedResult<VehicleProfileListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchProfiles(
        [FromQuery] string? vin,
        [FromQuery] string? year,
        [FromQuery] string? make,
        [FromQuery] string? model,
        [FromQuery] string? engine,
        [FromQuery] int? toolMilStatus,
        [FromQuery] int? toolLedStatus,
        [FromQuery] string? primaryDtc,
        [FromQuery] string? dtcCode,
        [FromQuery] string? dtcSource,
        [FromQuery] string? dtcSystem,
        [FromQuery] string? dtcStatusType,
        [FromQuery] string? dtcDefinition,
        [FromQuery] string? dtcFormat,
        [FromQuery] bool? hasRawBuffer,
        CancellationToken cancellationToken,
        [FromQuery] string? dtcPrefix = null,
        [FromQuery] string? primaryDtcMode = null,
        [FromQuery] string? dtcCodeMode = null,
        [FromQuery] int? yearFrom = null,
        [FromQuery] int? yearTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20
    ) =>
        HandleResult(
            await sender.Send(
                new SearchVehicleProfilesQuery(
                    vin, year, make, model, engine, toolMilStatus, toolLedStatus,
                    primaryDtc, dtcCode, dtcSource, dtcSystem, dtcStatusType, dtcDefinition, dtcFormat,
                    dtcPrefix, primaryDtcMode, dtcCodeMode, hasRawBuffer,
                    yearFrom, yearTo, sortBy, sortDirection, pageNumber, pageSize
                ),
                cancellationToken
            )
        );

    /// <summary>Distinct YMME options for the cascading filter (faceted by current selections).</summary>
    [HttpGet("ymme-options")]
    [ProducesResponseType(typeof(YmmeOptionsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetYmmeOptions(
        [FromQuery] string? year,
        [FromQuery] string? make,
        [FromQuery] string? model,
        [FromQuery] string? engine,
        CancellationToken cancellationToken
    ) => HandleResult(await sender.Send(new GetYmmeOptionsQuery(year, make, model, engine), cancellationToken));

    /// <summary>Full detail of one vehicle profile (snapshot + DTCs/monitors/freeze frames + payload/buffer metadata).</summary>
    [HttpGet("profiles/{id:long}")]
    [ProducesResponseType(typeof(VehicleProfileDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileById(long id, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new GetVehicleProfileByIdQuery(id), cancellationToken));

    /// <summary>Get one payload's content (decompressed) as base64.</summary>
    [HttpGet("payloads/{id:long}")]
    [ProducesResponseType(typeof(PayloadContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayloadContent(long id, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new GetPayloadContentQuery(id), cancellationToken));

    /// <summary>Get one raw buffer's content (decompressed) as base64.</summary>
    [HttpGet("raw-buffers/{id:long}")]
    [ProducesResponseType(typeof(RawBufferContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRawBufferContent(long id, CancellationToken cancellationToken) =>
        HandleResult(await sender.Send(new GetRawBufferContentQuery(id), cancellationToken));
}
