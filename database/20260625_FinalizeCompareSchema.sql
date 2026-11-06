/* =============================================================================
   Migration: 20260625_FinalizeCompareSchema
   Purpose  : Adapt the scaffolded Compare tables to the finalized design before
              the Compare Engine is implemented.

     - RENAME CompareResult.PassCount -> UnchangedCount (per-request result
       "Pass" is renamed to "Unchanged" for clearer meaning).
     - ADD    CompareJob.ProjectId (+ FK to Project, mirroring Run) so compares
       are scoped to the active project.
     - ADD    TABLE CompareJobApi: which APIs a compare targets (no rows = ALL
       APIs), mirroring RunApi.
     - INDEX  CompareJob (ProjectId, CreatedDate DESC, Id DESC) for the
       Compare History list (newest first, per project).

   Notes:
     - CompareResult has NO unique on (RunAId, RunBId): re-comparing the same
       pair intentionally creates a NEW result.
     - ModifiedCount is left in place (reserved; the finalized per-request result
       set is Unchanged / Changed / Added / Removed / Failed).

   Compare tables were scaffolded empty; no compute code depends on them yet.
   Forward-only and idempotent.
   ============================================================================= */

USE RegressionDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

/* 1) Rename PassCount -> UnchangedCount (a request whose response did not change). */
IF COL_LENGTH('dbo.CompareResult', 'PassCount') IS NOT NULL
   AND COL_LENGTH('dbo.CompareResult', 'UnchangedCount') IS NULL
    EXEC sp_rename 'dbo.CompareResult.PassCount', 'UnchangedCount', 'COLUMN';
GO

/* 2) Project scope on the compare job (+ FK to Project, mirroring Run). Table is empty. */
IF COL_LENGTH('dbo.CompareJob', 'ProjectId') IS NULL
    ALTER TABLE dbo.CompareJob ADD ProjectId INT NOT NULL;
GO

IF OBJECT_ID('dbo.FK_CompareJob_Project', 'F') IS NULL
    ALTER TABLE dbo.CompareJob
        ADD CONSTRAINT FK_CompareJob_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Project (Id);
GO

/* 3) APIs a compare targets (empty = all APIs), mirroring RunApi. */
IF OBJECT_ID('dbo.CompareJobApi', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompareJobApi
    (
        Id           BIGINT NOT NULL IDENTITY(1, 1),
        CompareJobId BIGINT NOT NULL,
        ApiId        INT    NOT NULL,
        CONSTRAINT PK_CompareJobApi PRIMARY KEY (Id),
        CONSTRAINT FK_CompareJobApi_Job FOREIGN KEY (CompareJobId) REFERENCES dbo.CompareJob (Id) ON DELETE CASCADE,
        CONSTRAINT FK_CompareJobApi_Api FOREIGN KEY (ApiId) REFERENCES dbo.Api (Id) ON DELETE CASCADE,
        CONSTRAINT UQ_CompareJobApi_Job_Api UNIQUE (CompareJobId, ApiId)
    );
END;
GO

/* 4) Compare History list: newest-first, per project. */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_CompareJob_Project' AND object_id = OBJECT_ID('dbo.CompareJob')
)
    CREATE INDEX IX_CompareJob_Project ON dbo.CompareJob (ProjectId, CreatedDate DESC, Id DESC);
GO

COMMIT TRANSACTION;
GO
