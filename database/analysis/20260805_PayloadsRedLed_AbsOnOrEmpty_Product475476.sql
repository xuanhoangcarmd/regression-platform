USE RegressionDB;
GO

/* ----------------------------------------------------------------------------
   Payloads matching:
     - ToolLEDStatus = Red            (enum Off=0, Green=1, Yellow=2, Red=3, Error=4  ->  Red = 3)
     - ABSLightStatus = 'ON'  OR NULL OR empty ('' / whitespace)
     - UsbProductId IN (475, 476)     (product id 0475 / 0476, stored as a decimal int)
   ---------------------------------------------------------------------------- */
SELECT
    p.Id             AS PayloadId,
    vp.Id            AS VehicleProfileId,
    vp.Vin,
    vp.UsbProductId,
    vp.ToolLEDStatus,
    vp.ABSLightStatus,
    vp.[Year],
    vp.Make,
    vp.Model,
    vp.EngineType,
    p.Name           AS PayloadName,
    p.ByteSize,
    p.Source,
    p.CreatedDate,
    -- The raw tool payload: stored gzip is decompressed (DECOMPRESS, SQL Server 2016+) then base64-encoded.
    CAST('' AS XML).value('xs:base64Binary(sql:column("d.RawBin"))', 'VARCHAR(MAX)') AS PayloadBase64
FROM Payload p
INNER JOIN VehicleProfile vp ON vp.Id = p.VehicleProfileId
CROSS APPLY (
    SELECT CASE WHEN p.Compression = 'gzip' THEN DECOMPRESS(p.PayloadData)
                ELSE CAST(p.PayloadData AS VARBINARY(MAX)) END AS RawBin
) d
WHERE vp.ToolLEDStatus = 3                                          -- Red
  AND (vp.ABSLightStatus = 'ON' OR vp.ABSLightStatus IS NULL OR LTRIM(RTRIM(vp.ABSLightStatus)) = '')
  AND vp.UsbProductId IN (475, 476)
ORDER BY p.Id;
GO
