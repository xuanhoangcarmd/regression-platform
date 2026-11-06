/* =============================================================================
   Migration: 20261106_RollbackExpectedFeatures
   Purpose  : Remove the "Generate Expected Run" feature (rolled back — did not meet the requirement),
              i.e. undo 20261106_AddExpectedRunGeneration.sql. Run this AFTER deploying the code
              without it.

   KEPT (not touched):
     - Expected Responses editor + compare baseline mode (20261007_AddExpectedResponses.sql):
       RunResponseOverride, CompareJob.BaselineMode, CompareDetail.IsEdited
     - Order mode (20261019) and the cut-at rule (20261020)

   DATA LOSS (by design): Change Specs and generated expectations. Compare results that used
   'ExpectationFailed' are converted to 'Changed' first, so existing compares / reports stay consistent.

   Idempotent (every step is guarded) — safe to rerun.
   ============================================================================= */

USE RegressionDB;
GO

-- Keep old compares readable: ExpectationFailed → Changed (and fold the count into ChangedCount).
IF COL_LENGTH('dbo.CompareResult', 'ExpectationFailedCount') IS NOT NULL
BEGIN
    EXEC('UPDATE dbo.CompareResult SET ChangedCount = ChangedCount + ExpectationFailedCount WHERE ExpectationFailedCount > 0;');
    UPDATE dbo.CompareDetail SET Result = 'Changed' WHERE Result = 'ExpectationFailed';
END;
GO

IF OBJECT_ID('dbo.RunExpectation', 'U') IS NOT NULL
    DROP TABLE dbo.RunExpectation;
GO

IF OBJECT_ID('dbo.FK_Run_ChangeSpec', 'F') IS NOT NULL
    ALTER TABLE dbo.Run DROP CONSTRAINT FK_Run_ChangeSpec;
IF OBJECT_ID('dbo.FK_Run_SourceRun', 'F') IS NOT NULL
    ALTER TABLE dbo.Run DROP CONSTRAINT FK_Run_SourceRun;
IF OBJECT_ID('dbo.CK_Run_GenerationBaseline', 'C') IS NOT NULL
    ALTER TABLE dbo.Run DROP CONSTRAINT CK_Run_GenerationBaseline;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Run_SourceRun' AND object_id = OBJECT_ID('dbo.Run'))
    DROP INDEX IX_Run_SourceRun ON dbo.Run;
GO

IF COL_LENGTH('dbo.Run', 'SourceRunId') IS NOT NULL
    ALTER TABLE dbo.Run DROP COLUMN SourceRunId;
IF COL_LENGTH('dbo.Run', 'ChangeSpecId') IS NOT NULL
    ALTER TABLE dbo.Run DROP COLUMN ChangeSpecId;
IF COL_LENGTH('dbo.Run', 'GenerationBaseline') IS NOT NULL
    ALTER TABLE dbo.Run DROP COLUMN GenerationBaseline;
GO

IF OBJECT_ID('dbo.ChangeSpec', 'U') IS NOT NULL
    DROP TABLE dbo.ChangeSpec;
GO

IF COL_LENGTH('dbo.RunResponse', 'ChangeOps') IS NOT NULL
    ALTER TABLE dbo.RunResponse DROP COLUMN ChangeOps;
GO

IF OBJECT_ID('dbo.DF_CompareResult_ExpectationFailedCount', 'D') IS NOT NULL
    ALTER TABLE dbo.CompareResult DROP CONSTRAINT DF_CompareResult_ExpectationFailedCount;
IF COL_LENGTH('dbo.CompareResult', 'ExpectationFailedCount') IS NOT NULL
    ALTER TABLE dbo.CompareResult DROP COLUMN ExpectationFailedCount;
GO

/* Optional: runs created by the generator (RunType = 'Generated') are ordinary Run rows now; their
   bodies may contain «expect: …» placeholders. Delete them from the Runs page, or uncomment below
   (fails if a compare / report still references one — delete those first).

-- DELETE FROM dbo.RunResponseOverride WHERE RunId IN (SELECT Id FROM dbo.Run WHERE RunType = 'Generated');
-- DELETE rr FROM dbo.RunResponse rr JOIN dbo.Run r ON r.Id = rr.RunId WHERE r.RunType = 'Generated';
-- DELETE FROM dbo.RunApi WHERE RunId IN (SELECT Id FROM dbo.Run WHERE RunType = 'Generated');
-- DELETE FROM dbo.Run WHERE RunType = 'Generated';
*/
