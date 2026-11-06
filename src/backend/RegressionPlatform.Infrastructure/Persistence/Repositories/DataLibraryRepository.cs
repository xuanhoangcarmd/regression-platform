using Dapper;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.DataLibrary;
using RegressionPlatform.Application.Features.DataLibrary.Contracts;
using RegressionPlatform.Infrastructure.Persistence.Sql;
using RegressionPlatform.Infrastructure.Services;
using RegressionPlatform.Shared.Pagination;

namespace RegressionPlatform.Infrastructure.Persistence.Repositories;

/// <summary>Dapper read-side data access for the Data Library.</summary>
public sealed class DataLibraryRepository(IDbConnectionFactory connectionFactory) : IDataLibraryRepository
{
    // Scalar profile row (column order matches DataLibrarySql.GetProfile).
    private sealed record ProfileRow(
        long Id,
        string? SourceId,
        string? Vin,
        string? Year,
        string? Make,
        string? Model,
        string? EngineType,
        string? Trim,
        string? Transmission,
        int? UsbProductId,
        int? ToolMilStatus,
        string? PrimaryDtc,
        int? ToolLedStatus,
        string? Sil,
        string? AbsLightStatus,
        string? TpmsLightStatus,
        string? BatteryConditionStatus,
        string? BatteryVoltage,
        int? Odometer,
        int? Mileage,
        bool HasRecordTime,
        bool HasFreezeFrames,
        bool HasMonitors,
        string? Notes
    );

    private sealed record PayloadContentRow(long Id, string? Name, byte[] PayloadData, string? Compression, int? ByteSize, string? Source);

    private sealed record RawBufferContentRow(long Id, string BufferType, int? SegmentIndex, byte[] RawData, string? Compression, int? ByteSize);

    public async Task<PagedResult<VehicleProfileListItem>> SearchProfilesAsync(
        VehicleProfileSearchParameters parameters,
        CancellationToken cancellationToken
    )
    {
        // DTC format applies to whichever DTC keyword has a value (folded in as "prefix + shape"); when
        // neither keyword is given it falls back to a shape-only match on the DTC code (the old rule).
        var format = Trimmed(parameters.DtcFormat);
        if (format is not null && !DataLibrarySql.DtcFormats.Contains(format))
        {
            format = null;
        }
        var hasPrimary = !string.IsNullOrWhiteSpace(parameters.PrimaryDtc);
        var hasCode = !string.IsNullOrWhiteSpace(parameters.DtcCode);

        var primaryPattern = hasPrimary && format is not null
            ? SqlSearch.FormatOverlay(format, parameters.PrimaryDtc)
            : SqlSearch.Pattern(parameters.PrimaryDtc, parameters.PrimaryDtcMode);
        var codePattern = hasCode && format is not null
            ? SqlSearch.FormatOverlay(format, parameters.DtcCode)
            : SqlSearch.Pattern(parameters.DtcCode, parameters.DtcCodeMode);
        // Shape-only fallback on the DTC code, only when a format is chosen and no keyword was given.
        var formatPattern = format is not null && !hasPrimary && !hasCode ? SqlSearch.FormatShape(format) : null;

        // Code-prefix category (P/B/C/U, 'number', 'not-pbcu') — passed as a whitelisted key the SQL branches on.
        var prefix = Trimmed(parameters.DtcPrefix);
        if (prefix is not null && !DataLibrarySql.DtcPrefixes.Contains(prefix))
        {
            prefix = null;
        }

        var queryParams = new
        {
            Vin = SqlSearch.Contains(parameters.Vin),
            Year = Trimmed(parameters.Year),
            Make = Trimmed(parameters.Make),
            Model = Trimmed(parameters.Model),
            Engine = Trimmed(parameters.Engine),
            MilStatus = parameters.ToolMilStatus,
            LedStatus = parameters.ToolLedStatus,
            PrimaryDtc = primaryPattern,
            DtcCode = codePattern,
            DtcSource = Trimmed(parameters.DtcSource),
            DtcSystem = SqlSearch.Contains(parameters.DtcSystem),
            DtcStatusType = SqlSearch.Contains(parameters.DtcStatusType),
            DtcDefinition = SqlSearch.Contains(parameters.DtcDefinition),
            DtcFormat = formatPattern,
            DtcPrefix = prefix,
            HasRawBuffer = parameters.HasRawBuffer,
            parameters.YearFrom,
            parameters.YearTo,
            Offset = (parameters.PageNumber - 1) * parameters.PageSize,
            parameters.PageSize,
        };

        var orderBy = BuildOrderBy(parameters.SortBy, parameters.SortDirection);
        var sql = DataLibrarySql.SearchCount
            + "\n"
            + DataLibrarySql.SearchSelect + " " + orderBy + DataLibrarySql.Paging;

        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, queryParams, cancellationToken: cancellationToken)
        );

        var totalCount = await multi.ReadFirstAsync<int>();
        var items = (await multi.ReadAsync<VehicleProfileListItem>()).ToList();
        return new PagedResult<VehicleProfileListItem>(items, parameters.PageNumber, parameters.PageSize, totalCount);
    }

    public async Task<YmmeOptionsResponse> GetYmmeOptionsAsync(
        string? year,
        string? make,
        string? model,
        string? engine,
        CancellationToken cancellationToken
    )
    {
        var queryParams = new
        {
            Year = Trimmed(year),
            Make = Trimmed(make),
            Model = Trimmed(model),
            Engine = Trimmed(engine),
        };

        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(DataLibrarySql.YmmeOptions, queryParams, cancellationToken: cancellationToken)
        );

        var years = (await multi.ReadAsync<string>()).ToList();
        var makes = (await multi.ReadAsync<string>()).ToList();
        var models = (await multi.ReadAsync<string>()).ToList();
        var engines = (await multi.ReadAsync<string>()).ToList();
        return new YmmeOptionsResponse(years, makes, models, engines);
    }

    public async Task<VehicleProfileDetailResponse?> GetProfileByIdAsync(long id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        var row = await connection.QuerySingleOrDefaultAsync<ProfileRow>(
            new CommandDefinition(DataLibrarySql.GetProfile, new { Id = id }, cancellationToken: cancellationToken)
        );
        if (row is null)
        {
            return null;
        }

        var dtcs = (await connection.QueryAsync<VehicleDtcItem>(
            new CommandDefinition(DataLibrarySql.GetDtcs, new { Id = id }, cancellationToken: cancellationToken))).ToList();
        var payloads = (await connection.QueryAsync<PayloadMetaItem>(
            new CommandDefinition(DataLibrarySql.GetPayloadsMeta, new { Id = id }, cancellationToken: cancellationToken))).ToList();
        var rawBuffers = (await connection.QueryAsync<RawBufferMetaItem>(
            new CommandDefinition(DataLibrarySql.GetRawBuffersMeta, new { Id = id }, cancellationToken: cancellationToken))).ToList();

        return new VehicleProfileDetailResponse(
            row.Id, row.SourceId, row.Vin, row.Year, row.Make, row.Model, row.EngineType, row.Trim, row.Transmission,
            row.UsbProductId, row.ToolMilStatus, row.PrimaryDtc, row.ToolLedStatus, row.Sil, row.AbsLightStatus,
            row.TpmsLightStatus, row.BatteryConditionStatus, row.BatteryVoltage, row.Odometer, row.Mileage,
            row.HasRecordTime, row.HasFreezeFrames, row.HasMonitors, row.Notes,
            dtcs, payloads, rawBuffers
        );
    }

    public async Task<PayloadContentResponse?> GetPayloadContentAsync(long id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<PayloadContentRow>(
            new CommandDefinition(DataLibrarySql.GetPayloadContent, new { Id = id }, cancellationToken: cancellationToken)
        );
        if (row is null)
        {
            return null;
        }

        var base64 = GzipCompressor.ToBase64(row.PayloadData, row.Compression);
        return new PayloadContentResponse(row.Id, row.Name, base64, row.ByteSize, row.Source);
    }

    public async Task<RawBufferContentResponse?> GetRawBufferContentAsync(long id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<RawBufferContentRow>(
            new CommandDefinition(DataLibrarySql.GetRawBufferContent, new { Id = id }, cancellationToken: cancellationToken)
        );
        if (row is null)
        {
            return null;
        }

        var base64 = GzipCompressor.ToBase64(row.RawData, row.Compression);
        return new RawBufferContentResponse(row.Id, row.BufferType, row.SegmentIndex, base64, row.ByteSize);
    }

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Builds a safe ORDER BY from a whitelisted sort key (+ asc/desc). Unknown/empty keys fall back
    /// to the default (newest first). Only expressions from <see cref="DataLibrarySql.SortColumns"/>
    /// are ever emitted, so the untrusted <paramref name="sortBy"/> can't inject SQL.
    /// </summary>
    private static string BuildOrderBy(string? sortBy, string? sortDirection)
    {
        if (string.IsNullOrWhiteSpace(sortBy) || !DataLibrarySql.SortColumns.TryGetValue(sortBy.Trim(), out var column))
        {
            return DataLibrarySql.DefaultOrderBy;
        }

        var direction = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        return $"ORDER BY {column} {direction}, vp.Id DESC";
    }
}
