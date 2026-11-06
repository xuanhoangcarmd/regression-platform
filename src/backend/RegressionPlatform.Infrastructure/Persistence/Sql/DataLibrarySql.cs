namespace RegressionPlatform.Infrastructure.Persistence.Sql;

/// <summary>Parameterized SQL for the Data Library (read side).</summary>
internal static class DataLibrarySql
{
    // Search filters. VIN + DTC code are "contains" (LIKE); YMME exact; MIL by ToolMilStatus (mapped int).
    private const string SearchFrom =
        "FROM VehicleProfile vp "
        + "WHERE vp.IsActive = 1 "
        + "AND (@Vin IS NULL OR vp.Vin LIKE @Vin ESCAPE '\\') "
        + "AND (@Year IS NULL OR vp.[Year] = @Year) "
        // Numeric year range (Year is stored as text; TRY_CAST ignores non-numeric values).
        + "AND (@YearFrom IS NULL OR TRY_CAST(vp.[Year] AS INT) >= @YearFrom) "
        + "AND (@YearTo IS NULL OR TRY_CAST(vp.[Year] AS INT) <= @YearTo) "
        + "AND (@Make IS NULL OR vp.Make = @Make) "
        + "AND (@Model IS NULL OR vp.Model = @Model) "
        + "AND (@Engine IS NULL OR vp.EngineType = @Engine) "
        + "AND (@MilStatus IS NULL OR vp.ToolMilStatus = @MilStatus) "
        + "AND (@LedStatus IS NULL OR vp.ToolLEDStatus = @LedStatus) "
        + "AND (@PrimaryDtc IS NULL OR vp.PrimaryDtc LIKE @PrimaryDtc ESCAPE '\\') "
        // The DTC filters must all match the SAME VehicleDtc row (one EXISTS). No DTC filter = no-op.
        + "AND ((@DtcCode IS NULL AND @DtcSource IS NULL AND @DtcSystem IS NULL "
        + "      AND @DtcStatusType IS NULL AND @DtcDefinition IS NULL AND @DtcFormat IS NULL AND @DtcPrefix IS NULL) "
        + "  OR EXISTS (SELECT 1 FROM VehicleDtc d WHERE d.VehicleProfileId = vp.Id "
        + "      AND (@DtcCode IS NULL OR d.Code LIKE @DtcCode ESCAPE '\\') "
        + "      AND (@DtcSource IS NULL OR d.Source = @DtcSource) "
        + "      AND (@DtcSystem IS NULL OR d.[System] LIKE @DtcSystem ESCAPE '\\') "
        + "      AND (@DtcStatusType IS NULL OR d.StatusType LIKE @DtcStatusType ESCAPE '\\') "
        + "      AND (@DtcDefinition IS NULL OR d.Definition LIKE @DtcDefinition ESCAPE '\\') "
        // Shape-only code-format fallback (used when no DTC keyword is given). The repository turns the
        // chosen shape into a LIKE pattern where each 'x' is [^-] (one non-dash char) and '-' is literal,
        // e.g. 'xxxx' -> '[^-][^-][^-][^-]'. When a keyword IS given the shape is folded into @DtcCode
        // (prefix + shape) upstream, so @DtcFormat is null here.
        + "      AND (@DtcFormat IS NULL OR d.Code LIKE @DtcFormat ESCAPE '\\') "
        // Code-prefix category (independent of shape/keyword). P/B/C/U = the DTC system letter;
        // 'number' = starts with a digit; 'not-pbcu' = anything not starting with P/B/C/U.
        + "      AND (@DtcPrefix IS NULL "
        + "        OR (@DtcPrefix = 'P' AND d.Code LIKE 'P%') "
        + "        OR (@DtcPrefix = 'B' AND d.Code LIKE 'B%') "
        + "        OR (@DtcPrefix = 'C' AND d.Code LIKE 'C%') "
        + "        OR (@DtcPrefix = 'U' AND d.Code LIKE 'U%') "
        + "        OR (@DtcPrefix = 'number' AND d.Code LIKE '[0-9]%') "
        // 'not-pbcu' also requires a real code (has a digit, no space) so text placeholders like
        // "No code" / "Available" don't slip in just because they don't start with P/B/C/U.
        + "        OR (@DtcPrefix = 'not-pbcu' AND d.Code NOT LIKE '[PBCU]%' "
        + "            AND d.Code LIKE '%[0-9]%' AND d.Code NOT LIKE '% %') "
        // 'not-pbcu-num' = starts with some other letter (not P/B/C/U and not a digit), still a real code.
        + "        OR (@DtcPrefix = 'not-pbcu-num' AND d.Code NOT LIKE '[PBCU]%' AND d.Code NOT LIKE '[0-9]%' "
        + "            AND d.Code LIKE '%[0-9]%' AND d.Code NOT LIKE '% %') "
        // 'not-junk' = real codes only, i.e. exclude text placeholders like "No code" / "Available".
        + "        OR (@DtcPrefix = 'not-junk' AND d.Code LIKE '%[0-9]%' AND d.Code NOT LIKE '% %')))) "
        // Has-raw-buffer filter: 1 = only profiles with buffers, 0 = only without, NULL = no filter.
        + "AND (@HasRawBuffer IS NULL "
        + "  OR (CASE WHEN EXISTS (SELECT 1 FROM RawBuffer rb WHERE rb.VehicleProfileId = vp.Id) THEN 1 ELSE 0 END) = @HasRawBuffer)";

    public const string SearchCount = $"SELECT COUNT(1) {SearchFrom};";

    // SELECT + FROM/WHERE only. The ORDER BY + paging is appended by the repository so the sort
    // column can be chosen dynamically from a whitelist (see DataLibraryRepository.BuildOrderBy).
    // Column order matches VehicleProfileListItem (Dapper binds positionally).
    public const string SearchSelect =
        "SELECT vp.Id, vp.Vin, vp.[Year], vp.Make, vp.Model, vp.EngineType, vp.ToolMilStatus, "
        + "(SELECT COUNT(1) FROM Payload p WHERE p.VehicleProfileId = vp.Id) AS PayloadCount, "
        + "(SELECT COUNT(1) FROM VehicleDtc d WHERE d.VehicleProfileId = vp.Id) AS DtcCount, "
        + "(SELECT COUNT(1) FROM RawBuffer rb WHERE rb.VehicleProfileId = vp.Id) AS RawBufferCount "
        + SearchFrom;

    /// <summary>Whitelisted sort columns (query key → safe SQL expression). Prevents SQL injection.</summary>
    public static readonly IReadOnlyDictionary<string, string> SortColumns = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["year"] = "TRY_CAST(vp.[Year] AS INT)",
        ["make"] = "vp.Make",
        ["model"] = "vp.Model",
        ["engine"] = "vp.EngineType",
        ["engineType"] = "vp.EngineType",
    };

    /// <summary>Whitelisted DTC code-format shapes (a shape = only 'x' and '-'). Anything else is ignored.</summary>
    public static readonly IReadOnlySet<string> DtcFormats = new HashSet<string>(StringComparer.Ordinal)
    {
        // No-dash shapes
        "xx",
        "xxxx",
        "xxxxx",
        "xxxxxx",
        "xxxxxxx",
        "xxxxxxxx",
        // Dashed shapes
        "x-x",
        "x-xx",
        "x-xxx",
        "xx-x",
        "xx-xx",
        "xx-xxx",
        "xxx-x",
        "xxx-xx",
        "xxxx-xxx",
        "xxxxx-xx",
        "xxxxx-xxx",
    };

    /// <summary>Whitelisted DTC code-prefix categories (must match the @DtcPrefix branches in the SQL above).</summary>
    public static readonly IReadOnlySet<string> DtcPrefixes = new HashSet<string>(StringComparer.Ordinal)
    {
        "P",
        "B",
        "C",
        "U",
        "number",
        "not-pbcu",
        "not-pbcu-num",
        "not-junk",
    };

    public const string DefaultOrderBy = "ORDER BY vp.CreatedDate DESC, vp.Id DESC";
    public const string Paging = " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

    // Profile scalar row (column order matches the private ProfileRow record).
    public const string GetProfile =
        "SELECT Id, SourceId, Vin, [Year], Make, Model, EngineType, Trim, Transmission, UsbProductId, ToolMilStatus, "
        + "PrimaryDtc, ToolLEDStatus, Sil, ABSLightStatus, TPMSLightStatus, BatteryConditionStatus, BatteryVoltage, "
        + "Odometer, Mileage, HasRecordTime, HasFreezeFrames, HasMonitors, Notes FROM VehicleProfile WHERE Id = @Id;";

    // Faceted YMME options: each list = distinct non-null values matching the OTHER selected facets
    // (so picking one narrows the rest). Returns 4 result sets: Years, Makes, Models, Engines.
    public const string YmmeOptions =
        "SELECT DISTINCT [Year] FROM VehicleProfile WHERE IsActive = 1 AND [Year] IS NOT NULL "
        + "AND (@Make IS NULL OR Make = @Make) AND (@Model IS NULL OR Model = @Model) "
        + "AND (@Engine IS NULL OR EngineType = @Engine) ORDER BY [Year];\n"
        + "SELECT DISTINCT Make FROM VehicleProfile WHERE IsActive = 1 AND Make IS NOT NULL "
        + "AND (@Year IS NULL OR [Year] = @Year) AND (@Model IS NULL OR Model = @Model) "
        + "AND (@Engine IS NULL OR EngineType = @Engine) ORDER BY Make;\n"
        + "SELECT DISTINCT Model FROM VehicleProfile WHERE IsActive = 1 AND Model IS NOT NULL "
        + "AND (@Year IS NULL OR [Year] = @Year) AND (@Make IS NULL OR Make = @Make) "
        + "AND (@Engine IS NULL OR EngineType = @Engine) ORDER BY Model;\n"
        + "SELECT DISTINCT EngineType FROM VehicleProfile WHERE IsActive = 1 AND EngineType IS NOT NULL "
        + "AND (@Year IS NULL OR [Year] = @Year) AND (@Make IS NULL OR Make = @Make) "
        + "AND (@Model IS NULL OR Model = @Model) ORDER BY EngineType;";

    public const string GetDtcs =
        "SELECT Id, Source, Code, StatusType, [System], SubSystem, Definition "
        + "FROM VehicleDtc WHERE VehicleProfileId = @Id ORDER BY Id;";

    public const string GetPayloadsMeta =
        "SELECT Id, Name, ByteSize, Source, CreatedDate FROM Payload WHERE VehicleProfileId = @Id ORDER BY Id;";

    public const string GetRawBuffersMeta =
        "SELECT Id, BufferType, SegmentIndex, ByteSize FROM RawBuffer WHERE VehicleProfileId = @Id "
        + "ORDER BY BufferType, SegmentIndex, Id;";

    public const string GetPayloadContent =
        "SELECT Id, Name, PayloadData, Compression, ByteSize, Source FROM Payload WHERE Id = @Id;";

    public const string GetRawBufferContent =
        "SELECT Id, BufferType, SegmentIndex, RawData, Compression, ByteSize FROM RawBuffer WHERE Id = @Id;";
}
