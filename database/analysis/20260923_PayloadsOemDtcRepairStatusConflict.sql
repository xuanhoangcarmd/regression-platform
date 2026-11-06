USE RegressionDB;
GO

/* ----------------------------------------------------------------------------
   Payloads with a DUPLICATED OEM DTC whose RepairStatus DIFFERS between the copies,
   restricted to DTCs whose Group is NOT OBDII.

   TWO fields here are NOT stored in the database and have to be derived. Read this
   before trusting the output.

   (A) RepairStatus — VehicleDtc has no RepairStatus column. It is computed downstream
       from StatusType; the only in-repo source is the bucket comments in
       src/frontend/.../payload-generator/constants/payload-enums.const.ts
       ("Types ... determines RepairStatus on the backend").

       StatusType is pipe-delimited and can carry several tokens at once
       (e.g. 'MIL|History|Current'), so the buckets are applied with this precedence:

           Recommend Repair  >  Warning  >  History  >  Unknown

       That precedence is an ASSUMPTION — the real DtcParser lives in the product
       backend, not in this repo. If it differs, change the CASE in OemDtc, nothing else.

   (B) Group — the payload generator gives an OEM DTC a Group (the Innova group byte:
       OBDII / ABS / SRS / TCM / PCM / OBDI / TPMS / ADAS) SEPARATELY from System
       (see payload-generator-modal.component.html, the System and Group combos).
       The importer stores only SystemName / SubsystemName / Definition / Types into
       VehicleDtc (import_payloads.py) — THE GROUP BYTE IS DROPPED. The Payload blob is
       a binary tool payload, not JSON, so SQL cannot recover it either.

       GroupGuess below is therefore a PROXY derived from the System text. It is a
       heuristic, not the real group. Only 3 OEM rows in the whole table have an
       OBDII-looking System, so "GroupGuess <> 'OBDII'" on its own is nearly a no-op —
       the default filter also drops PCM/powertrain, which is what the OBDII group
       actually covers on the tool. Adjust the Filtered CTE to taste.
   ---------------------------------------------------------------------------- */

/* ============================================================================
   1) Per-code view — one row per (payload, conflicting code), with the detail.
   ============================================================================ */
WITH OemDtc AS (
    SELECT
        d.VehicleProfileId,
        d.Code,
        d.StatusType,
        d.[System],
        d.SubSystem,
        d.Definition,
        -- Proxy for the Innova group byte. Order matters: most specific module first.
        GroupGuess = CASE
            WHEN d.[System] LIKE '%OBD%' THEN 'OBDII'
            WHEN d.[System] LIKE '%TPMS%' OR d.[System] LIKE '%Tire Pressure%'
              OR d.[System] LIKE '%Tyre Pressure%' OR d.[System] LIKE '%RDC%' THEN 'TPMS'
            WHEN d.[System] LIKE '%SRS%' OR d.[System] LIKE '%Airbag%'
              OR d.[System] LIKE '%Air Bag%' OR d.[System] LIKE '%Restraint%' THEN 'SRS'
            WHEN d.[System] LIKE '%ABS%' OR d.[System] LIKE '%Anti-Lock%' OR d.[System] LIKE '%Anti Lock%'
              OR d.[System] LIKE '%Stability%' OR d.[System] LIKE '%ESC%' OR d.[System] LIKE '%DSC%'
              OR d.[System] LIKE '%VSC%' OR d.[System] LIKE '%VDC%' OR d.[System] LIKE '%Brake%' THEN 'ABS'
            WHEN d.[System] LIKE '%ADAS%' OR d.[System] LIKE '%Driver assistance%'
              OR d.[System] LIKE '%Lane%' OR d.[System] LIKE '%Collision%' THEN 'ADAS'
            WHEN d.[System] LIKE '%TCM%' OR d.[System] LIKE '%Transmission%'
              OR d.[System] LIKE '%Transaxle%' OR d.[System] LIKE '%EGS%' THEN 'TCM'
            WHEN d.[System] LIKE '%PCM%' OR d.[System] LIKE '%Powertrain%' OR d.[System] LIKE '%Engine%'
              OR d.[System] LIKE '%ECM%' OR d.[System] LIKE '%ECT%' THEN 'PCM'
            WHEN d.[System] LIKE '%Body%' OR d.[System] LIKE '%BdyCM%' OR d.[System] LIKE '%CEM%'
              OR d.[System] LIKE '%Instrument%' OR d.[System] LIKE '%IPC%' THEN 'Body'
            WHEN d.[System] LIKE '%Chassis%' THEN 'Chassis'
            ELSE 'Other'
        END,
        RepairStatus = CASE
            WHEN EXISTS (SELECT 1 FROM STRING_SPLIT(d.StatusType, '|') s WHERE LTRIM(RTRIM(s.value)) IN (
                '1 Trip', '1t', 'Active', 'Confirmed', 'Current', 'Current/MIL', 'Failsafe/MIL',
                'Indeterminate/MIL', 'Intermittent/MIL', 'Last Test Failed', 'MIL', 'Not Current/MIL',
                'Permanent', 'Present', 'Static', 'Test Failed', 'This Ignition Cycle'))
                THEN 'Recommend Repair'
            WHEN EXISTS (SELECT 1 FROM STRING_SPLIT(d.StatusType, '|') s WHERE LTRIM(RTRIM(s.value)) IN (
                'DEVELOPING', 'FSC', 'Indeterminate', 'Intermittent', 'Not Confirmed', 'Not Passed SC',
                'Pending', 'Temporary', 'Unconfirmed',
                -- BufferName enum values — bucketed Warning, not classified by DtcParser
                'CM', 'Continuous', 'DCheck', 'Failed', 'Invalid', 'KOEO', 'KOER', 'Latched',
                'Memory', 'Provisional', 'Readiness'))
                THEN 'Warning'
            WHEN EXISTS (SELECT 1 FROM STRING_SPLIT(d.StatusType, '|') s WHERE LTRIM(RTRIM(s.value)) IN (
                'Historic', 'History', 'Not Active', 'Not Current', 'Not Present', 'Past',
                'Storage', 'Stored'))
                THEN 'History'
            ELSE 'Unknown'
        END
    FROM dbo.VehicleDtc d
    WHERE d.Source = 'OEM'
      AND d.StatusType IS NOT NULL
      AND d.StatusType <> ''          -- no StatusType = no basis for a RepairStatus (~9.6k OEM rows)
),
Filtered AS (
    -- The "Group is not OBDII" filter. Tune this one line:
    --   NOT IN ('OBDII', 'PCM')  -> exclude OBDII + powertrain      (default)
    --   <> 'OBDII'               -> literal, but near no-op (3 rows)
    --   IN ('ABS', 'TPMS', ...)  -> keep only specific groups
    SELECT * FROM OemDtc WHERE GroupGuess NOT IN ('OBDII', 'PCM')
),
Conflict AS (
    -- Same code in the same group, more than one copy, copies do NOT agree on RepairStatus.
    SELECT
        VehicleProfileId,
        Code,
        GroupGuess,
        COUNT(1)                     AS Copies,
        COUNT(DISTINCT RepairStatus) AS DistinctRepairStatus
    FROM Filtered
    GROUP BY VehicleProfileId, Code, GroupGuess
    HAVING COUNT(1) > 1
       AND COUNT(DISTINCT RepairStatus) > 1
)
SELECT
    p.Id                AS PayloadId,
    c.VehicleProfileId,
    vp.Vin,
    vp.[Year],
    vp.Make,
    vp.Model,
    c.Code,
    c.GroupGuess,
    c.Copies,
    c.DistinctRepairStatus,
    -- e.g. 'Current -> Recommend Repair  ||  History -> History  ||  Pending -> Warning'
    (SELECT STRING_AGG(CONCAT(f.StatusType, ' -> ', f.RepairStatus), '  ||  ')
     FROM Filtered f
     WHERE f.VehicleProfileId = c.VehicleProfileId AND f.Code = c.Code AND f.GroupGuess = c.GroupGuess) AS Detail,
    (SELECT TOP 1 f.[System]
     FROM Filtered f
     WHERE f.VehicleProfileId = c.VehicleProfileId AND f.Code = c.Code AND f.GroupGuess = c.GroupGuess) AS SystemName,
    p.Name              AS PayloadName,
    p.ByteSize,
    p.Source            AS PayloadSource,
    p.CreatedDate
FROM Conflict c
INNER JOIN dbo.VehicleProfile vp ON vp.Id = c.VehicleProfileId
INNER JOIN dbo.Payload        p  ON p.VehicleProfileId = vp.Id
-- WHERE vp.IsActive = 1     -- uncomment to match the Data Library view (active profiles only)
ORDER BY c.DistinctRepairStatus DESC, c.Copies DESC, p.Id;
GO


/* ============================================================================
   2) Per-payload rollup — for picking ONE sample that exercises the conflict a lot.
      Same CTEs, aggregated per profile.
   ============================================================================ */
WITH OemDtc AS (
    SELECT
        d.VehicleProfileId,
        d.Code,
        GroupGuess = CASE
            WHEN d.[System] LIKE '%OBD%' THEN 'OBDII'
            WHEN d.[System] LIKE '%TPMS%' OR d.[System] LIKE '%Tire Pressure%'
              OR d.[System] LIKE '%Tyre Pressure%' OR d.[System] LIKE '%RDC%' THEN 'TPMS'
            WHEN d.[System] LIKE '%SRS%' OR d.[System] LIKE '%Airbag%'
              OR d.[System] LIKE '%Air Bag%' OR d.[System] LIKE '%Restraint%' THEN 'SRS'
            WHEN d.[System] LIKE '%ABS%' OR d.[System] LIKE '%Anti-Lock%' OR d.[System] LIKE '%Anti Lock%'
              OR d.[System] LIKE '%Stability%' OR d.[System] LIKE '%ESC%' OR d.[System] LIKE '%DSC%'
              OR d.[System] LIKE '%VSC%' OR d.[System] LIKE '%VDC%' OR d.[System] LIKE '%Brake%' THEN 'ABS'
            WHEN d.[System] LIKE '%ADAS%' OR d.[System] LIKE '%Driver assistance%'
              OR d.[System] LIKE '%Lane%' OR d.[System] LIKE '%Collision%' THEN 'ADAS'
            WHEN d.[System] LIKE '%TCM%' OR d.[System] LIKE '%Transmission%'
              OR d.[System] LIKE '%Transaxle%' OR d.[System] LIKE '%EGS%' THEN 'TCM'
            WHEN d.[System] LIKE '%PCM%' OR d.[System] LIKE '%Powertrain%' OR d.[System] LIKE '%Engine%'
              OR d.[System] LIKE '%ECM%' OR d.[System] LIKE '%ECT%' THEN 'PCM'
            WHEN d.[System] LIKE '%Body%' OR d.[System] LIKE '%BdyCM%' OR d.[System] LIKE '%CEM%'
              OR d.[System] LIKE '%Instrument%' OR d.[System] LIKE '%IPC%' THEN 'Body'
            WHEN d.[System] LIKE '%Chassis%' THEN 'Chassis'
            ELSE 'Other'
        END,
        RepairStatus = CASE
            WHEN EXISTS (SELECT 1 FROM STRING_SPLIT(d.StatusType, '|') s WHERE LTRIM(RTRIM(s.value)) IN (
                '1 Trip', '1t', 'Active', 'Confirmed', 'Current', 'Current/MIL', 'Failsafe/MIL',
                'Indeterminate/MIL', 'Intermittent/MIL', 'Last Test Failed', 'MIL', 'Not Current/MIL',
                'Permanent', 'Present', 'Static', 'Test Failed', 'This Ignition Cycle'))
                THEN 'Recommend Repair'
            WHEN EXISTS (SELECT 1 FROM STRING_SPLIT(d.StatusType, '|') s WHERE LTRIM(RTRIM(s.value)) IN (
                'DEVELOPING', 'FSC', 'Indeterminate', 'Intermittent', 'Not Confirmed', 'Not Passed SC',
                'Pending', 'Temporary', 'Unconfirmed',
                'CM', 'Continuous', 'DCheck', 'Failed', 'Invalid', 'KOEO', 'KOER', 'Latched',
                'Memory', 'Provisional', 'Readiness'))
                THEN 'Warning'
            WHEN EXISTS (SELECT 1 FROM STRING_SPLIT(d.StatusType, '|') s WHERE LTRIM(RTRIM(s.value)) IN (
                'Historic', 'History', 'Not Active', 'Not Current', 'Not Present', 'Past',
                'Storage', 'Stored'))
                THEN 'History'
            ELSE 'Unknown'
        END
    FROM dbo.VehicleDtc d
    WHERE d.Source = 'OEM' AND d.StatusType IS NOT NULL AND d.StatusType <> ''
),
Filtered AS (
    SELECT * FROM OemDtc WHERE GroupGuess NOT IN ('OBDII', 'PCM')
),
Conflict AS (
    SELECT VehicleProfileId, Code, GroupGuess,
           COUNT(1) AS Copies, COUNT(DISTINCT RepairStatus) AS DistinctRepairStatus
    FROM Filtered
    GROUP BY VehicleProfileId, Code, GroupGuess
    HAVING COUNT(1) > 1 AND COUNT(DISTINCT RepairStatus) > 1
),
ProfileRollup AS (
    SELECT
        VehicleProfileId,
        COUNT(1)                  AS ConflictCodes,   -- how many codes conflict
        SUM(Copies)               AS ConflictRows,    -- total DTC rows involved
        MAX(DistinctRepairStatus) AS MaxDistinct,     -- 3 = one code hits Repair + Warning + History
        STRING_AGG(GroupGuess, ', ') WITHIN GROUP (ORDER BY GroupGuess) AS Groups
    FROM Conflict
    GROUP BY VehicleProfileId
)
SELECT
    p.Id            AS PayloadId,
    r.VehicleProfileId,
    vp.Vin,
    vp.[Year],
    vp.Make,
    vp.Model,
    r.ConflictCodes,
    r.MaxDistinct,
    r.ConflictRows,
    r.Groups,
    p.ByteSize,
    p.Source        AS PayloadSource
FROM ProfileRollup r
INNER JOIN dbo.VehicleProfile vp ON vp.Id = r.VehicleProfileId
INNER JOIN dbo.Payload        p  ON p.VehicleProfileId = vp.Id
ORDER BY r.ConflictCodes DESC, r.MaxDistinct DESC, p.Id;
GO
