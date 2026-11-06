namespace RegressionPlatform.Application.Features.DataLibrary.Contracts;

/// <summary>Full detail of a vehicle profile: scalar snapshot + DTCs + payload/buffer metadata.</summary>
public sealed record VehicleProfileDetailResponse(
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
    string? Notes,
    IReadOnlyList<VehicleDtcItem> Dtcs,
    IReadOnlyList<PayloadMetaItem> Payloads,
    IReadOnlyList<RawBufferMetaItem> RawBuffers
);

public sealed record VehicleDtcItem(
    long Id,
    string Source,
    string Code,
    string? StatusType,
    string? System,
    string? SubSystem,
    string? Definition
);

/// <summary>Payload metadata for the detail list (no binary content; fetch content separately).</summary>
public sealed record PayloadMetaItem(long Id, string? Name, int? ByteSize, string? Source, DateTime CreatedDate);

/// <summary>Raw buffer metadata for the detail list (no binary content; fetch content separately).</summary>
public sealed record RawBufferMetaItem(long Id, string BufferType, int? SegmentIndex, int? ByteSize);
