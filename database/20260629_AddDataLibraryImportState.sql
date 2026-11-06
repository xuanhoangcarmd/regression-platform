/* =============================================================================
   Migration: 20260629_AddDataLibraryImportState
   Purpose  : Support the LIVE → Data Library import (Python ETL):
     - DataLibraryImportState : per-job checkpoint so the import can resume where it
       left off (it walks DiagnosticReport newest → oldest).
     - Payload.PayloadHash    : SHA-256 of the (uncompressed) payload + a filtered
       UNIQUE index, so a duplicate payload is skipped (not re-imported).
     - VehicleProfile.SourceId: the source DiagnosticReport id (provenance / debug).

   Forward-only and idempotent. Run AFTER 20260629_CreateDataLibrary.sql.
   ============================================================================= */

USE RegressionDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

/* 1) Import checkpoint (one row per named job). */
IF OBJECT_ID('dbo.DataLibraryImportState', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DataLibraryImportState
    (
        Id                     INT           NOT NULL IDENTITY(1, 1),
        JobName                NVARCHAR(100) NOT NULL,                 -- e.g. 'live-diagnostic-report'
        LastCreatedDateTimeUtc DATETIME2     NULL,                     -- cursor: last processed report time
        LastSourceId           NVARCHAR(100) NULL,                     -- cursor tiebreaker: last processed report id
        ProcessedCount         INT           NOT NULL CONSTRAINT DF_DataLibraryImportState_Processed DEFAULT (0),
        ImportedCount          INT           NOT NULL CONSTRAINT DF_DataLibraryImportState_Imported DEFAULT (0),
        SkippedCount           INT           NOT NULL CONSTRAINT DF_DataLibraryImportState_Skipped DEFAULT (0),
        Status                 NVARCHAR(20)  NOT NULL CONSTRAINT DF_DataLibraryImportState_Status DEFAULT ('Idle'),
        CreatedDate            DATETIME2     NOT NULL CONSTRAINT DF_DataLibraryImportState_CreatedDate DEFAULT (getutcdate()),
        UpdatedDate            DATETIME2     NULL,
        CONSTRAINT PK_DataLibraryImportState PRIMARY KEY (Id),
        CONSTRAINT UQ_DataLibraryImportState_JobName UNIQUE (JobName)
    );
END;
GO

/* 2) Payload de-dup: hash + filtered unique index (only enforced for rows that set a hash). */
IF COL_LENGTH('dbo.Payload', 'PayloadHash') IS NULL
    ALTER TABLE dbo.Payload ADD PayloadHash CHAR(64) NULL;  -- SHA-256 hex of the uncompressed payload
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Payload_Hash' AND object_id = OBJECT_ID('dbo.Payload'))
    CREATE UNIQUE INDEX UQ_Payload_Hash ON dbo.Payload (PayloadHash) WHERE PayloadHash IS NOT NULL;
GO

/* 3) Provenance: which source report a profile came from. */
IF COL_LENGTH('dbo.VehicleProfile', 'SourceId') IS NULL
    ALTER TABLE dbo.VehicleProfile ADD SourceId NVARCHAR(100) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VehicleProfile_SourceId' AND object_id = OBJECT_ID('dbo.VehicleProfile'))
    CREATE INDEX IX_VehicleProfile_SourceId ON dbo.VehicleProfile (SourceId);
GO

COMMIT TRANSACTION;
GO
