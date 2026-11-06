USE RegressionDB;
GO

/* ----------------------------------------------------------------------------
   Payloads whose vehicle profile has MORE THAN 30 DTCs.

   DTCs live in VehicleDtc (one row per code, linked by VehicleProfileId); payloads live in
   Payload (also linked by VehicleProfileId). A profile can have several payloads — each is
   returned, all sharing that profile's DTC count.

   Change the threshold in the HAVING clause (> 30) as needed.
   ---------------------------------------------------------------------------- */
WITH DtcCounts AS (
    SELECT d.VehicleProfileId, COUNT(1) AS DtcCount
    FROM VehicleDtc d
    GROUP BY d.VehicleProfileId
    HAVING COUNT(1) > 30
)
SELECT
    p.Id             AS PayloadId,
    vp.Id            AS VehicleProfileId,
    vp.Vin,
    vp.[Year],
    vp.Make,
    vp.Model,
    vp.EngineType,
    dc.DtcCount,
    p.Name           AS PayloadName,
    p.ByteSize,
    p.Source,
    p.CreatedDate
FROM DtcCounts dc
INNER JOIN VehicleProfile vp ON vp.Id = dc.VehicleProfileId
INNER JOIN Payload        p  ON p.VehicleProfileId = vp.Id
-- AND vp.IsActive = 1        -- uncomment to match the Data Library view (active profiles only)
ORDER BY dc.DtcCount DESC, p.Id;
GO
