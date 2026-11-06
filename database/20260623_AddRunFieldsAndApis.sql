/* =============================================================================
   Migration: 20260623_AddRunFieldsAndApis
   Purpose  : Support creating runs from the UI.

     - ADD Run.Environment (DEV/STG/PROD) and Run.RunType (Sprint/Hotfix/…).
     - ADD TABLE RunApi: which APIs a run targets (no link rows = ALL APIs).

   Run execution is NOT implemented yet — these just persist a created run definition.
   Forward-only and idempotent.
   ============================================================================= */

USE RegressionDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

/* 1) New Run columns (table is empty / new runs always set them; defaults cover any existing rows). */
IF COL_LENGTH('dbo.Run', 'Environment') IS NULL
    ALTER TABLE dbo.Run ADD Environment VARCHAR(20) NOT NULL CONSTRAINT DF_Run_Environment DEFAULT ('DEV');
IF COL_LENGTH('dbo.Run', 'RunType') IS NULL
    ALTER TABLE dbo.Run ADD RunType VARCHAR(20) NOT NULL CONSTRAINT DF_Run_RunType DEFAULT ('Sprint');
GO

/* 2) Junction: APIs a run targets (empty = all APIs). */
IF OBJECT_ID('dbo.RunApi', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RunApi
    (
        Id    BIGINT NOT NULL IDENTITY(1, 1),
        RunId BIGINT NOT NULL,
        ApiId INT    NOT NULL,
        CONSTRAINT PK_RunApi PRIMARY KEY (Id),
        CONSTRAINT FK_RunApi_Run FOREIGN KEY (RunId) REFERENCES dbo.Run (Id) ON DELETE CASCADE,
        CONSTRAINT FK_RunApi_Api FOREIGN KEY (ApiId) REFERENCES dbo.Api (Id) ON DELETE CASCADE,
        CONSTRAINT UQ_RunApi_Run_Api UNIQUE (RunId, ApiId)
    );
END;
GO

COMMIT TRANSACTION;
GO
