/* =============================================================================
   Migration: 20260624_AddRunResponseHeaders
   Purpose  : Persist the captured response headers for each executed request so
              the Run Results screen can show a "Headers" tab.

     - ADD RunResponse.ResponseHeaders (NVARCHAR(MAX) NULL) — a JSON object of
       header name → value (response + content headers), captured by the executor.
       NULL when the request errored before a response was received.

   Forward-only and idempotent.
   ============================================================================= */

USE RegressionDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.RunResponse', 'ResponseHeaders') IS NULL
    ALTER TABLE dbo.RunResponse ADD ResponseHeaders NVARCHAR(MAX) NULL;

COMMIT TRANSACTION;
GO
