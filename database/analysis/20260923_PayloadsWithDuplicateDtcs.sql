USE RegressionDB;
GO

/* ----------------------------------------------------------------------------
   Payloads whose DTC list contains DUPLICATES — most duplicates first.

   DTCs live in VehicleDtc (one row per code, linked by VehicleProfileId); payloads live in
   Payload (also linked by VehicleProfileId). "The payload's DTC list" is therefore its
   profile's VehicleDtc rows. A profile can have several payloads — each is returned, all
   sharing that profile's counts.

   Two different notions of "duplicate" — pick the one you need:

     DupByCode  = same Code appears more than once, whatever the status.
                  Mostly BENIGN: OBDII repeats a code across StatusType
                  (Pending / Permanent / Stored). Highest values come from here.
     DupExact   = the whole row repeats (Code + Source + System + SubSystem + StatusType).
                  This is a REAL duplicate — usually an import/parsing artifact.
     MaxSameRow = how many times the worst identical row repeats (2 = a pair, 4 = four copies).

   Default ordering is DupExact first (real duplicates), then DupByCode.
   To chase the biggest lists regardless of kind, swap the ORDER BY (see note below).
   ---------------------------------------------------------------------------- */
WITH DtcStats AS (
    SELECT
        d.VehicleProfileId,
        COUNT(1)                          AS TotalDtc,
        COUNT(DISTINCT d.Code)            AS DistinctCode,
        COUNT(1) - COUNT(DISTINCT d.Code) AS DupByCode
    FROM dbo.VehicleDtc d
    GROUP BY d.VehicleProfileId
),
ExactDup AS (
    -- Group identical rows, then count the extra copies (Cnt - 1) per profile.
    SELECT VehicleProfileId, SUM(Cnt - 1) AS DupExact, MAX(Cnt) AS MaxSameRow
    FROM (
        SELECT d.VehicleProfileId, COUNT(1) AS Cnt
        FROM dbo.VehicleDtc d
        GROUP BY d.VehicleProfileId, d.Code, d.Source, d.[System], d.SubSystem, d.StatusType
    ) g
    GROUP BY VehicleProfileId
)
SELECT
    p.Id                        AS PayloadId,
    p.VehicleProfileId,
    vp.Vin,
    vp.[Year],
    vp.Make,
    vp.Model,
    vp.EngineType,
    s.TotalDtc,
    s.DistinctCode,
    s.DupByCode,
    ISNULL(e.DupExact, 0)       AS DupExact,
    ISNULL(e.MaxSameRow, 1)     AS MaxSameRow,
    p.Name                      AS PayloadName,
    p.ByteSize,
    p.Source,
    p.CreatedDate
FROM DtcStats s
INNER JOIN dbo.VehicleProfile vp ON vp.Id = s.VehicleProfileId
INNER JOIN dbo.Payload        p  ON p.VehicleProfileId = vp.Id
LEFT  JOIN ExactDup           e  ON e.VehicleProfileId = s.VehicleProfileId
WHERE s.DupByCode > 0
-- AND ISNULL(e.DupExact, 0) > 0   -- uncomment: only REAL (whole-row) duplicates
-- AND vp.IsActive = 1             -- uncomment to match the Data Library view (active profiles only)
ORDER BY DupExact DESC, s.DupByCode DESC, s.TotalDtc DESC, p.Id;
-- Swap for "most repeated codes overall": ORDER BY s.DupByCode DESC, DupExact DESC, p.Id;
GO


/* ----------------------------------------------------------------------------
   Drill-down: the duplicate rows of ONE profile. Set @VehicleProfileId first.
   ---------------------------------------------------------------------------- */
DECLARE @VehicleProfileId BIGINT = 1451;   -- e.g. the top DupExact profile

SELECT
    d.Code,
    d.Source,
    d.[System],
    d.SubSystem,
    d.StatusType,
    COUNT(1) AS Copies
FROM dbo.VehicleDtc d
WHERE d.VehicleProfileId = @VehicleProfileId
GROUP BY d.Code, d.Source, d.[System], d.SubSystem, d.StatusType
HAVING COUNT(1) > 1          -- drop this line to see the full list, duplicates included
ORDER BY Copies DESC, d.Code;
GO
