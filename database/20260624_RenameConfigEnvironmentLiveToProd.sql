/* =============================================================================
   Migration: 20260624_RenameConfigEnvironmentLiveToProd
   Purpose  : Standardize configuration environments on DEV / STG / PROD.
              The original CHECK allowed DEV/STG/LIVE; the app now uses PROD.

     - Drop the old CHECK constraint.
     - Migrate existing rows: Environment 'LIVE' → 'PROD'.
     - Re-add the CHECK allowing DEV/STG/PROD.

   Forward-only and idempotent.
   ============================================================================= */

USE RegressionDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

/* 1) Drop the old environment CHECK (if present). */
IF OBJECT_ID('dbo.CK_ProjectConfiguration_Environment', 'C') IS NOT NULL
    ALTER TABLE dbo.ProjectConfiguration DROP CONSTRAINT CK_ProjectConfiguration_Environment;

/* 2) Migrate existing data. */
UPDATE dbo.ProjectConfiguration SET Environment = 'PROD' WHERE Environment = 'LIVE';

/* 3) Re-add the CHECK with the new allowed set. */
IF OBJECT_ID('dbo.CK_ProjectConfiguration_Environment', 'C') IS NULL
    ALTER TABLE dbo.ProjectConfiguration
        ADD CONSTRAINT CK_ProjectConfiguration_Environment CHECK (Environment IN ('DEV', 'STG', 'PROD'));

COMMIT TRANSACTION;
GO
