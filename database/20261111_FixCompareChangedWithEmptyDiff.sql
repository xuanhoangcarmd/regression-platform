/*
    Data fix: compare details marked "Changed" with an EMPTY diff.

    Cause (fixed in code by b04571c "Treat empty diff as Unchanged in ignore-order compares"):
    in ignore-order mode the canonical hash sorts object keys but not array elements, so a response whose
    primitive array was only reordered (e.g. DTCCodes ["P0742","P0741"] vs ["P0741","P0742"]) hashed
    differently and was saved as Changed, while the differ found no difference (DiffJson = NULL).

    What it does, for ONE compare job (set @CompareJobId below):
      1. CompareDetail: Result 'Changed' -> 'Unchanged' where DiffJson is empty, the detail is not edited
         and both runs returned the same status code.
      2. CompareResult: UnchangedCount / ChangedCount recounted from CompareDetail.
      3. Report (if any): counts copied from CompareResult, RegressionScore recomputed
         (Unchanged / Total * 100, 1 decimal). RiskLevel / BreakingApiCount don't change (an empty diff
         has no FieldRemoved / TypeChanged entries).

    Safe to re-run (idempotent). Runs in a transaction; review the PREVIEW output first.
    Other affected compares at the time of writing: 108, 109, 110, 111.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @CompareJobId BIGINT = 169;

DECLARE @CompareResultId BIGINT, @RunAId BIGINT, @RunBId BIGINT;
SELECT @CompareResultId = CompareResultId, @RunAId = RunAId, @RunBId = RunBId
FROM CompareJob
WHERE Id = @CompareJobId;

IF @CompareResultId IS NULL
BEGIN
    RAISERROR('Compare job %I64d not found or has no result.', 16, 1, @CompareJobId);
    RETURN;
END;

-- PREVIEW: details that will be changed.
SELECT d.Id AS CompareDetailId, d.RequestId, d.Result, ra.StatusCode AS StatusA, rb.StatusCode AS StatusB
FROM CompareDetail d
LEFT JOIN RunResponse ra ON ra.RunId = @RunAId AND ra.RequestId = d.RequestId
LEFT JOIN RunResponse rb ON rb.RunId = @RunBId AND rb.RequestId = d.RequestId
WHERE d.CompareResultId = @CompareResultId
  AND d.Result = 'Changed'
  AND (d.DiffJson IS NULL OR LTRIM(RTRIM(d.DiffJson)) IN ('', '[]'))
  AND d.IsEdited = 0
  AND ra.StatusCode = rb.StatusCode;

BEGIN TRANSACTION;

    -- 1. Details.
    UPDATE d
    SET d.Result = 'Unchanged',
        d.DiffJson = NULL
    FROM CompareDetail d
    INNER JOIN RunResponse ra ON ra.RunId = @RunAId AND ra.RequestId = d.RequestId
    INNER JOIN RunResponse rb ON rb.RunId = @RunBId AND rb.RequestId = d.RequestId
    WHERE d.CompareResultId = @CompareResultId
      AND d.Result = 'Changed'
      AND (d.DiffJson IS NULL OR LTRIM(RTRIM(d.DiffJson)) IN ('', '[]'))
      AND d.IsEdited = 0
      AND ra.StatusCode = rb.StatusCode;

    PRINT CONCAT('CompareDetail rows fixed: ', @@ROWCOUNT);

    -- 2. Compare result counts, recounted from the details.
    UPDATE r
    SET r.UnchangedCount = c.UnchangedCount,
        r.ChangedCount = c.ChangedCount
    FROM CompareResult r
    CROSS APPLY (
        SELECT
            SUM(CASE WHEN d.Result = 'Unchanged' THEN 1 ELSE 0 END) AS UnchangedCount,
            SUM(CASE WHEN d.Result = 'Changed' THEN 1 ELSE 0 END) AS ChangedCount
        FROM CompareDetail d
        WHERE d.CompareResultId = r.Id
    ) c
    WHERE r.Id = @CompareResultId;

    -- 3. Report snapshot (same formula as GenerateReportCommandHandler.ComputeScore).
    UPDATE rp
    SET rp.UnchangedCount = r.UnchangedCount,
        rp.ChangedCount = r.ChangedCount,
        rp.RegressionScore = CASE
            WHEN ISNULL(r.TotalRequest, 0) <= 0 THEN 100
            ELSE ROUND(CAST(r.UnchangedCount AS DECIMAL(18, 6)) / r.TotalRequest * 100, 1)
        END
    FROM Report rp
    INNER JOIN CompareResult r ON r.Id = @CompareResultId
    WHERE rp.CompareResultId = @CompareResultId;

    PRINT CONCAT('Report rows updated: ', @@ROWCOUNT);

COMMIT TRANSACTION;

-- RESULT: verify.
SELECT Id, TotalRequest, UnchangedCount, ChangedCount, FailedCount, AddedCount, RemovedCount
FROM CompareResult
WHERE Id = @CompareResultId;

SELECT Id, TotalRequest, UnchangedCount, ChangedCount, RegressionScore, RiskLevel
FROM Report
WHERE CompareResultId = @CompareResultId;
