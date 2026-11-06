/* =============================================================================
   Migration: 20260624_AddRunRequestLimit
   Purpose  : Optional per-API request cap for a run.

     - ADD Run.RequestLimitPerApi (INT NULL). NULL = run all requests of each API;
       a value N = run at most N requests per API.

   Forward-only and idempotent.
   ============================================================================= */

USE RegressionDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Run', 'RequestLimitPerApi') IS NULL
    ALTER TABLE dbo.Run ADD RequestLimitPerApi INT NULL;

COMMIT TRANSACTION;
GO
