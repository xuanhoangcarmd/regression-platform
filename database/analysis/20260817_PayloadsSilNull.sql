USE RegressionDB;
GO

/* ----------------------------------------------------------------------------
   Payloads matching:
     - Sil IS NULL   (SIL status unknown / not captured)
   ---------------------------------------------------------------------------- */
SELECT
    p.Id             AS PayloadId,
    vp.Id            AS VehicleProfileId,
    vp.Vin,
    vp.UsbProductId,
    vp.Sil,
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
WHERE vp.Sil IS NULL
ORDER BY p.Id;
GO
