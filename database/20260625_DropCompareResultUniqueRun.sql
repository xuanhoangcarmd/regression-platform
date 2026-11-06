/* =============================================================================
   Migration: 20260625_DropCompareResultUniqueRun
   Purpose  : Allow re-comparing the same pair of runs.

     Drops the UNIQUE index UX_Compare_Run on CompareResult (RunAId, RunBId).
     Re-running a compare for the same (Run A, Run B) intentionally creates a NEW
     CompareResult, so the pair must NOT be unique.

   (This was a unique INDEX, not a constraint, so it was missed by the earlier
   FinalizeCompareSchema migration.)

   Forward-only and idempotent.
   ============================================================================= */

USE RegressionDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_Compare_Run' AND object_id = OBJECT_ID('dbo.CompareResult')
)
    DROP INDEX UX_Compare_Run ON dbo.CompareResult;
GO

COMMIT TRANSACTION;
GO
