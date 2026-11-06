/* =============================================================================
   Migration: 20260624_AddRunMetrics
   Purpose  : Store per-run execution health (computed when a run completes).

     - ADD Run.PassCount / ChangedCount / FailedCount (INT NULL).
       Pass  = response OK (HTTP < 400) and hash matches baseline (or no baseline).
       Changed = response OK but hash differs from the baseline run.
       Failed  = request errored (network) or HTTP >= 400.
       (Baseline = most recent Completed run in the same project + environment.)

   Forward-only and idempotent.
   ============================================================================= */

USE RegressionDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Run', 'PassCount') IS NULL ALTER TABLE dbo.Run ADD PassCount INT NULL;
IF COL_LENGTH('dbo.Run', 'ChangedCount') IS NULL ALTER TABLE dbo.Run ADD ChangedCount INT NULL;
IF COL_LENGTH('dbo.Run', 'FailedCount') IS NULL ALTER TABLE dbo.Run ADD FailedCount INT NULL;

COMMIT TRANSACTION;
GO
