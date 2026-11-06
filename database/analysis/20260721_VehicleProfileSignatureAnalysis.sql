/* =============================================================================
   Analysis: VehicleProfile diagnostic dataset — DISTINCT → LIST → FIND
   Purpose : READ-ONLY analysis (only creates #temp tables). DTC-centric:
             the case identity is the ACTUAL DTC CODE SET + vehicle diagnostic
             state (MIL, lights, FF/monitor flags, buffers). YMME is metadata
             only — it never participates in dedup.

   Workflow (mirrors how the dataset will be assembled):
     STEP 1 — DISTINCT : two profiles with the same DTC codes AND the same
               diagnostic state are the same data → keep 1 representative.
               Because the exact code set is part of the key, the kept set
               covers 100% of DTC codes BY CONSTRUCTION — no separate
               "must-include" pass is needed.
     STEP 2 — LIST     : catalog the distinct data by criteria (per DTC code,
               per diagnostic state) — the browsing index for picking cases.
     STEP 3 — FIND     : parameterized search — given criteria (code, MIL,
               FF, …), return matching profiles ranked richest-first.

   Report sections:
     (1) Summary          — total vs distinct-exact vs distinct-coarse
     (2) Exact duplicates — groups of identical data (safe to drop)
     (3a) List by DTC code            — each code: profiles that carry it
     (3b) List by diagnostic state    — each MIL/LED/FF/monitor combo: count
     (4) Draft dataset    — 1 representative per exact signature
     (5) FIND template    — edit the @variables and run the last query alone

   First run on 133 imported profiles (2026-07-21): 124 distinct exact code
   sets; 94 distinct coarse signatures. Re-run after the full ~5k import.

   NOTE: the 20260630 rebuild dropped VehicleMonitor / VehicleFreezeFrame —
   only the HasMonitors / HasFreezeFrames bits remain. If monitor readiness or
   FF content should be a deeper axis, the import must re-add detail.

   Schema per 20260630_RebuildDataLibrary.sql.
   ============================================================================= */

USE RegressionDB;
GO

SET NOCOUNT ON;

/* Drop in a separate batch (GO) — if a previous run left temp tables with a
   different shape, later statements would otherwise compile against the stale
   schema and fail with "Invalid column name". */
DROP TABLE IF EXISTS #ProfileSignature;
GO

/* ---------------------------------------------------------------------------
   STEP 1 — build one row per active profile:
     DtcCodeSet : ordered, comma-separated distinct DTC codes (the heart)
     SigDiag    : coarse diagnostic-state signature (grouping / browsing)
     SigHash    : SHA-256 of DtcCodeSet + SigDiag = the exact dedup key
   Every component is NULL-safe ('~' = missing) so positions never shift.
   --------------------------------------------------------------------------- */
WITH CodeSet AS (
    SELECT VehicleProfileId,
           STRING_AGG(CONVERT(nvarchar(max), Code), ',') WITHIN GROUP (ORDER BY Code) AS DtcCodeSet
    FROM (SELECT DISTINCT VehicleProfileId, Code FROM dbo.VehicleDtc) d
    GROUP BY VehicleProfileId
),
DtcCount AS (
    SELECT VehicleProfileId, COUNT(DISTINCT Code) AS DtcCount
    FROM dbo.VehicleDtc
    GROUP BY VehicleProfileId
),
DtcSourceSet AS (            -- ECU / TCU / OEM mix
    SELECT VehicleProfileId,
           STRING_AGG(Source, '+') WITHIN GROUP (ORDER BY Source) AS SourceSet
    FROM (SELECT DISTINCT VehicleProfileId, Source FROM dbo.VehicleDtc) d
    GROUP BY VehicleProfileId
),
DtcStatusSet AS (            -- normalized status buckets present
    SELECT VehicleProfileId,
           STRING_AGG(StatusBucket, '+') WITHIN GROUP (ORDER BY StatusBucket) AS StatusSet
    FROM (SELECT DISTINCT VehicleProfileId,
                 CASE WHEN StatusType LIKE '%MIL%' OR StatusType LIKE '%Current%' THEN 'CUR'
                      WHEN StatusType LIKE '%Pend%'                               THEN 'PEND'
                      WHEN StatusType LIKE '%Perm%'                               THEN 'PERM'
                      WHEN StatusType LIKE '%Stor%'                               THEN 'STOR'
                      WHEN StatusType IS NULL                                     THEN '~'
                      ELSE 'OTH' END AS StatusBucket
          FROM dbo.VehicleDtc) d
    GROUP BY VehicleProfileId
),
BufferSet AS (               -- which raw-buffer types exist (VinProfileRaw, DtcsEcmRaw, …)
    SELECT VehicleProfileId,
           STRING_AGG(BufferType, '+') WITHIN GROUP (ORDER BY BufferType) AS BufferSet,
           COUNT(*) AS BufferTypeCount
    FROM (SELECT DISTINCT VehicleProfileId, BufferType FROM dbo.RawBuffer) b
    GROUP BY VehicleProfileId
),
PayloadAgg AS (
    SELECT VehicleProfileId, COUNT(*) AS PayloadCount, MAX(ByteSize) AS MaxByteSize
    FROM dbo.Payload
    GROUP BY VehicleProfileId
)
SELECT
    p.Id,
    p.Vin, p.Make, p.Model, p.[Year], p.EngineType,   -- metadata only, NOT in the key
    p.ToolMilStatus, p.PrimaryDtc,
    p.HasRecordTime, p.HasFreezeFrames, p.HasMonitors,
    ISNULL(dc.DtcCount, 0)        AS DtcCount,
    ISNULL(bs.BufferTypeCount, 0) AS BufferTypeCount,
    ISNULL(pa.PayloadCount, 0)    AS PayloadCount,
    ISNULL(pa.MaxByteSize, 0)     AS MaxByteSize,
    ISNULL(cs.DtcCodeSet, '-')    AS DtcCodeSet,
    CONCAT_WS('|',
        /* --- tool / vehicle diagnostic state --- */
        CONCAT('MIL:',  ISNULL(CONVERT(varchar(11), p.ToolMilStatus), '~')),
        CONCAT('LED:',  ISNULL(CONVERT(varchar(11), p.ToolLEDStatus), '~')),
        CONCAT('SIL:',  ISNULL(p.Sil, '~')),
        CONCAT('ABS:',  ISNULL(p.ABSLightStatus, '~')),
        CONCAT('TPMS:', ISNULL(p.TPMSLightStatus, '~')),
        CONCAT('BAT:',  ISNULL(p.BatteryConditionStatus, '~')),
        /* --- record-time / freeze-frame / monitor presence --- */
        CONCAT('F:', CONVERT(char(1), p.HasRecordTime),
                     CONVERT(char(1), p.HasFreezeFrames),
                     CONVERT(char(1), p.HasMonitors)),
        /* --- primary DTC (the MIL-causing code, exact) --- */
        CONCAT('PDTC:', ISNULL(p.PrimaryDtc, '~')),
        /* --- DTC composition --- */
        CONCAT('DTC#:', CASE WHEN dc.DtcCount IS NULL THEN '0'
                             WHEN dc.DtcCount = 1  THEN '1'
                             WHEN dc.DtcCount <= 5 THEN '2-5'
                             ELSE '5+' END),
        CONCAT('SRC:', ISNULL(dsrc.SourceSet, '-')),
        CONCAT('ST:',  ISNULL(dst.StatusSet, '-')),
        /* --- data richness --- */
        CONCAT('BUF:', ISNULL(bs.BufferSet, '-')),
        CONCAT('PAY:', CASE WHEN pa.MaxByteSize IS NULL      THEN '~'
                            WHEN pa.MaxByteSize < 50000      THEN 'S'
                            WHEN pa.MaxByteSize < 200000     THEN 'M'
                            WHEN pa.MaxByteSize < 1000000    THEN 'L'
                            ELSE 'XL' END)
    ) AS SigDiag,
    HASHBYTES('SHA2_256',
        CONCAT(ISNULL(cs.DtcCodeSet, '-'), N'||',
               /* recompute the same SigDiag content for the hash */
               ISNULL(CONVERT(varchar(11), p.ToolMilStatus), '~'), '|',
               ISNULL(CONVERT(varchar(11), p.ToolLEDStatus), '~'), '|',
               ISNULL(p.Sil, '~'), '|', ISNULL(p.ABSLightStatus, '~'), '|',
               ISNULL(p.TPMSLightStatus, '~'), '|', ISNULL(p.BatteryConditionStatus, '~'), '|',
               CONVERT(char(1), p.HasRecordTime), CONVERT(char(1), p.HasFreezeFrames), CONVERT(char(1), p.HasMonitors), '|',
               ISNULL(p.PrimaryDtc, '~'), '|',
               ISNULL(dsrc.SourceSet, '-'), '|', ISNULL(dst.StatusSet, '-'), '|',
               ISNULL(bs.BufferSet, '-'))
    ) AS SigHash
INTO #ProfileSignature
FROM dbo.VehicleProfile p
LEFT JOIN CodeSet      cs   ON cs.VehicleProfileId   = p.Id
LEFT JOIN DtcCount     dc   ON dc.VehicleProfileId   = p.Id
LEFT JOIN DtcSourceSet dsrc ON dsrc.VehicleProfileId = p.Id
LEFT JOIN DtcStatusSet dst  ON dst.VehicleProfileId  = p.Id
LEFT JOIN BufferSet    bs   ON bs.VehicleProfileId   = p.Id
LEFT JOIN PayloadAgg   pa   ON pa.VehicleProfileId   = p.Id
WHERE p.IsActive = 1;
GO

/* ===========================================================================
   (1) Summary — the compression ladder:
       TotalProfiles ≥ DistinctExact (same codes + same state)
                     ≥ DistinctCoarse (same diagnostic shape, codes ignored)
       DistinctExact is the safe dedup (zero information loss, 100% code
       coverage). DistinctCoarse shows how much further a criteria-level
       grouping could compress if the budget demands it.
   =========================================================================== */
SELECT
    COUNT(*)                          AS TotalProfiles,
    COUNT(DISTINCT SigHash)           AS DistinctExact,
    COUNT(DISTINCT SigDiag)           AS DistinctCoarse,
    (SELECT COUNT(DISTINCT Code) FROM dbo.VehicleDtc) AS TotalDtcCodes
FROM #ProfileSignature;

/* ===========================================================================
   (2) Exact duplicates — groups of identical data. Everything after the
       first member of each group is redundant and safe to drop.
   =========================================================================== */
SELECT g.ProfileCount,
       g.ProfileIds,
       s.DtcCodeSet,
       s.SigDiag
FROM (
    SELECT SigHash,
           COUNT(*) AS ProfileCount,
           MIN(Id)  AS FirstId,
           STRING_AGG(CONVERT(nvarchar(max), Id), ', ') AS ProfileIds
    FROM #ProfileSignature
    GROUP BY SigHash
    HAVING COUNT(*) > 1
) g
JOIN #ProfileSignature s ON s.Id = g.FirstId
ORDER BY g.ProfileCount DESC;

/* ===========================================================================
   (3a) LIST by DTC code — the criteria catalog, DTC-first: every distinct
        code, how many profiles carry it, and up to 5 sample profile ids.
        Rare codes (1 profile) are pinned cases; frequent codes give you a
        choice of carrier.
   =========================================================================== */
SELECT d.Code,
       COUNT(DISTINCT d.VehicleProfileId) AS ProfileCount,
       (SELECT STRING_AGG(CONVERT(varchar(20), Id), ', ')
        FROM (SELECT TOP (5) VehicleProfileId AS Id
              FROM dbo.VehicleDtc x
              WHERE x.Code = d.Code
              GROUP BY VehicleProfileId
              ORDER BY VehicleProfileId) s)  AS SampleProfileIds
FROM dbo.VehicleDtc d
GROUP BY d.Code
ORDER BY COUNT(DISTINCT d.VehicleProfileId) ASC, d.Code;

/* ===========================================================================
   (3b) LIST by diagnostic state — how the data spreads across MIL / FF /
        monitor combos; empty combos = coverage gaps in the source data.
   =========================================================================== */
SELECT ISNULL(CONVERT(varchar(11), ToolMilStatus), '~') AS Mil,
       HasFreezeFrames, HasMonitors, HasRecordTime,
       CASE WHEN DtcCount = 0 THEN '0' WHEN DtcCount = 1 THEN '1'
            WHEN DtcCount <= 5 THEN '2-5' ELSE '5+' END AS DtcBucket,
       COUNT(*)                 AS Profiles,
       COUNT(DISTINCT SigHash)  AS DistinctData
FROM #ProfileSignature
GROUP BY ISNULL(CONVERT(varchar(11), ToolMilStatus), '~'),
         HasFreezeFrames, HasMonitors, HasRecordTime,
         CASE WHEN DtcCount = 0 THEN '0' WHEN DtcCount = 1 THEN '1'
              WHEN DtcCount <= 5 THEN '2-5' ELSE '5+' END
ORDER BY Profiles DESC;

/* ===========================================================================
   (4) Draft dataset — 1 representative per exact signature, richest first.
       By construction this set covers every DTC code and every diagnostic
       state present in the library. YMME columns are metadata for reading.
   =========================================================================== */
SELECT Id, Vin, Make, Model, [Year], EngineType,
       DtcCount, BufferTypeCount, PayloadCount, MaxByteSize,
       DtcCodeSet, SigDiag
FROM (
    SELECT *,
           ROW_NUMBER() OVER (
               PARTITION BY SigHash
               ORDER BY BufferTypeCount DESC, MaxByteSize DESC, Id) AS rn
    FROM #ProfileSignature
) r
WHERE rn = 1
ORDER BY DtcCount DESC, Id;

/* ===========================================================================
   (5) FIND — parameterized search. Set the variables (leave NULL to skip a
       criterion) and run this section alone. Results are ranked richest-first
       so the top row is the best payload to pull for that case.
   =========================================================================== */
DECLARE @DtcCode        nvarchar(50) = NULL;   -- e.g. N'P0420' (profile must carry it)
DECLARE @PrimaryDtc     nvarchar(50) = NULL;   -- e.g. N'P0300' (exact MIL-causing code)
DECLARE @ToolMilStatus  int          = NULL;   -- 1 = On, 3 = Off, 0 = Unknown
DECLARE @HasFreezeFrames bit         = NULL;
DECLARE @HasMonitors    bit          = NULL;
DECLARE @MinDtcCount    int          = NULL;   -- e.g. 5 (rich multi-DTC cases)
DECLARE @BufferType     nvarchar(50) = NULL;   -- e.g. N'FreezeFrameEcmRaw'

SELECT s.Id, s.Vin, s.Make, s.Model, s.[Year], s.EngineType,
       s.ToolMilStatus, s.PrimaryDtc, s.DtcCount,
       s.BufferTypeCount, s.PayloadCount, s.MaxByteSize,
       s.DtcCodeSet
FROM #ProfileSignature s
WHERE (@DtcCode IS NULL
       OR EXISTS (SELECT 1 FROM dbo.VehicleDtc d
                  WHERE d.VehicleProfileId = s.Id AND d.Code = @DtcCode))
  AND (@PrimaryDtc      IS NULL OR s.PrimaryDtc = @PrimaryDtc)
  AND (@ToolMilStatus   IS NULL OR s.ToolMilStatus = @ToolMilStatus)
  AND (@HasFreezeFrames IS NULL OR s.HasFreezeFrames = @HasFreezeFrames)
  AND (@HasMonitors     IS NULL OR s.HasMonitors = @HasMonitors)
  AND (@MinDtcCount     IS NULL OR s.DtcCount >= @MinDtcCount)
  AND (@BufferType IS NULL
       OR EXISTS (SELECT 1 FROM dbo.RawBuffer b
                  WHERE b.VehicleProfileId = s.Id AND b.BufferType = @BufferType))
ORDER BY s.BufferTypeCount DESC, s.DtcCount DESC, s.MaxByteSize DESC;
