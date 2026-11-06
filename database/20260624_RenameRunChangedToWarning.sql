/* =============================================================================
   Migration: 20260624_RenameRunChangedToWarning
   Purpose  : Replace the "Changed" run metric with "Warning".

     - RENAME Run.ChangedCount → Run.WarningCount.
       Warning = a request that responded OK (HTTP < 400) but slower than 2000ms.
       (Pass = OK and <= 2000ms; Fail = error or HTTP >= 400.)

   Forward-only and idempotent.
   ============================================================================= */

USE RegressionDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Run', 'ChangedCount') IS NOT NULL AND COL_LENGTH('dbo.Run', 'WarningCount') IS NULL
    EXEC sp_rename 'dbo.Run.ChangedCount', 'WarningCount', 'COLUMN';

COMMIT TRANSACTION;
GO
