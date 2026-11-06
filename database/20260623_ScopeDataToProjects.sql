/* =============================================================================
   Migration: 20260623_ScopeDataToProjects
   Purpose  : Make core data project-scoped. Adds ProjectId to the ROOT tables;
              children inherit their project through their parent:
                Api               + ProjectId   (ApiRequest inherits via Api)
                CompareIgnoreRule + ProjectId   (CompareIgnoreRuleApi inherits via Api)
                Run               + ProjectId   (RunResponse/CompareResult/CompareJob/CompareDetail inherit via Run)

   Existing data (15 Api, 5 CompareIgnoreRule) is backfilled to project 'oreilly'.
   Run and the compare tables are empty.

   Also: Api.Name becomes unique PER PROJECT (was globally unique).

   FK delete behavior = NO ACTION (business data; projects are soft-deleted via IsActive).
   Forward-only and idempotent.
   ============================================================================= */

USE RegressionDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

/* 1) Add nullable ProjectId columns. */
IF COL_LENGTH('dbo.Api', 'ProjectId') IS NULL ALTER TABLE dbo.Api ADD ProjectId INT NULL;
IF COL_LENGTH('dbo.CompareIgnoreRule', 'ProjectId') IS NULL ALTER TABLE dbo.CompareIgnoreRule ADD ProjectId INT NULL;
IF COL_LENGTH('dbo.Run', 'ProjectId') IS NULL ALTER TABLE dbo.Run ADD ProjectId INT NULL;
GO

/* 2) Backfill existing rows to 'oreilly'. */
DECLARE @oreilly INT = (SELECT Id FROM dbo.Project WHERE Code = 'oreilly');
IF @oreilly IS NULL
    THROW 50001, 'Project ''oreilly'' not found. Run 20260623_Create_ProjectConfiguration.sql first.', 1;

UPDATE dbo.Api SET ProjectId = @oreilly WHERE ProjectId IS NULL;
UPDATE dbo.CompareIgnoreRule SET ProjectId = @oreilly WHERE ProjectId IS NULL;
-- Run is empty; nothing to backfill.
GO

/* 3) Enforce NOT NULL. */
ALTER TABLE dbo.Api ALTER COLUMN ProjectId INT NOT NULL;
ALTER TABLE dbo.CompareIgnoreRule ALTER COLUMN ProjectId INT NOT NULL;
ALTER TABLE dbo.Run ALTER COLUMN ProjectId INT NOT NULL;
GO

/* 4) Foreign keys + supporting indexes. */
IF OBJECT_ID('dbo.FK_Api_Project', 'F') IS NULL
    ALTER TABLE dbo.Api ADD CONSTRAINT FK_Api_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Project (Id);
IF OBJECT_ID('dbo.FK_CompareIgnoreRule_Project', 'F') IS NULL
    ALTER TABLE dbo.CompareIgnoreRule ADD CONSTRAINT FK_CompareIgnoreRule_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Project (Id);
IF OBJECT_ID('dbo.FK_Run_Project', 'F') IS NULL
    ALTER TABLE dbo.Run ADD CONSTRAINT FK_Run_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Project (Id);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CompareIgnoreRule_ProjectId' AND object_id = OBJECT_ID('dbo.CompareIgnoreRule'))
    CREATE INDEX IX_CompareIgnoreRule_ProjectId ON dbo.CompareIgnoreRule (ProjectId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Run_ProjectId' AND object_id = OBJECT_ID('dbo.Run'))
    CREATE INDEX IX_Run_ProjectId ON dbo.Run (ProjectId);
GO

/* 5) Api.Name becomes unique per project (drop the global unique index).
      The new (ProjectId, Name) index also serves as the FK index on ProjectId. */
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Api_Name' AND object_id = OBJECT_ID('dbo.Api'))
    DROP INDEX IX_Api_Name ON dbo.Api;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Api_Project_Name' AND object_id = OBJECT_ID('dbo.Api'))
    CREATE UNIQUE INDEX UQ_Api_Project_Name ON dbo.Api (ProjectId, Name);
GO

COMMIT TRANSACTION;
GO
