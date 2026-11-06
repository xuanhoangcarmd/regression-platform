namespace RegressionPlatform.Application.Features.DataLibrary.Contracts;

/// <summary>Normalized filter/paging inputs for the Data Library search.</summary>
public sealed record VehicleProfileSearchParameters(
    string? Vin,
    string? Year,
    string? Make,
    string? Model,
    string? Engine,
    int? ToolMilStatus,
    int? ToolLedStatus,
    string? PrimaryDtc,
    string? DtcCode,
    string? DtcSource,
    string? DtcSystem,
    string? DtcStatusType,
    string? DtcDefinition,
    string? DtcFormat,
    string? DtcPrefix,
    string? PrimaryDtcMode,
    string? DtcCodeMode,
    bool? HasRawBuffer,
    int? YearFrom,
    int? YearTo,
    string? SortBy,
    string? SortDirection,
    int PageNumber,
    int PageSize
);

/// <summary>A row in the Data Library search results (profile summary + counts).</summary>
public sealed record VehicleProfileListItem(
    long Id,
    string? Vin,
    string? Year,
    string? Make,
    string? Model,
    string? EngineType,
    int? ToolMilStatus,
    int PayloadCount,
    int DtcCount,
    int RawBufferCount
);

/// <summary>Distinct YMME options (from VehicleProfile data) for the cascading filter; nulls excluded.</summary>
public sealed record YmmeOptionsResponse(
    IReadOnlyList<string> Years,
    IReadOnlyList<string> Makes,
    IReadOnlyList<string> Models,
    IReadOnlyList<string> Engines
);

/// <summary>A payload's decompressed content (base64) + metadata.</summary>
public sealed record PayloadContentResponse(long Id, string? Name, string Base64, int? ByteSize, string? Source);

/// <summary>A raw buffer's decompressed content (base64) + metadata.</summary>
public sealed record RawBufferContentResponse(long Id, string BufferType, int? SegmentIndex, string Base64, int? ByteSize);
