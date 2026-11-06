/* =============================================================================
   Migration: 20260629_AddDataset
   Purpose  : Introduce the Dataset concept so each team can own its own set of
              test requests, instead of "1 API = 1 fixed pool".

     - CREATE TABLE Dataset: a named collection of requests, scoped to a Project,
       spanning many APIs (e.g. "Mobile Regression"). Mirrors Project's columns.
     - ADD    ApiRequest.DatasetId (FK -> Dataset): every request now belongs to
       exactly one dataset (partition). A request still keeps its ApiId.
     - ADD    Run.DatasetId (FK -> Dataset): a run executes exactly one dataset
       (the API scope in RunApi narrows within that dataset).

   Backfill (zero-downtime, additive then enforced):
     - Create one "Default" dataset per Project.
     - Point every existing ApiRequest at its project's Default dataset
       (resolved via Api.ProjectId).
     - Point every existing Run at its project's Default dataset (Run.ProjectId).

   Invariant (enforced in the application layer, not by FK): a request's dataset
   and its API must belong to the same Project.

   Forward-only and idempotent.
   ============================================================================= */

USE RegressionDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

/* 1) Dataset table (named request collection, per project). */
IF OBJECT_ID('dbo.Dataset', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Dataset
    (
        Id          INT            NOT NULL IDENTITY(1, 1),
        ProjectId   INT            NOT NULL,
        Name        NVARCHAR(200)  NOT NULL,
        Description  NVARCHAR(1000) NULL,
        IsActive    BIT            NOT NULL CONSTRAINT DF_Dataset_IsActive DEFAULT (1),
        CreatedDate DATETIME2      NOT NULL CONSTRAINT DF_Dataset_CreatedDate DEFAULT (getutcdate()),
        UpdatedDate DATETIME2      NULL,
        CONSTRAINT PK_Dataset PRIMARY KEY (Id),
        CONSTRAINT FK_Dataset_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Project (Id),
        CONSTRAINT UQ_Dataset_Project_Name UNIQUE (ProjectId, Name)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Dataset_Project' AND object_id = OBJECT_ID('dbo.Dataset')
)
    CREATE INDEX IX_Dataset_Project ON dbo.Dataset (ProjectId);
GO

/* 2) One "Default" dataset per project (the bucket for pre-existing requests/runs). */
INSERT INTO dbo.Dataset (ProjectId, Name, Description)
SELECT p.Id, N'Default', N'Default dataset (auto-created during the Dataset migration).'
FROM dbo.Project p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Dataset d WHERE d.ProjectId = p.Id AND d.Name = N'Default'
);
GO

/* 3) ApiRequest.DatasetId: add nullable, backfill, then enforce NOT NULL + FK. */
IF COL_LENGTH('dbo.ApiRequest', 'DatasetId') IS NULL
    ALTER TABLE dbo.ApiRequest ADD DatasetId INT NULL;
GO

UPDATE r
SET r.DatasetId = d.Id
FROM dbo.ApiRequest r
INNER JOIN dbo.Api a ON a.Id = r.ApiId
INNER JOIN dbo.Dataset d ON d.ProjectId = a.ProjectId AND d.Name = N'Default'
WHERE r.DatasetId IS NULL;
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ApiRequest') AND name = 'DatasetId' AND is_nullable = 1
)
    ALTER TABLE dbo.ApiRequest ALTER COLUMN DatasetId INT NOT NULL;
GO

IF OBJECT_ID('dbo.FK_ApiRequest_Dataset', 'F') IS NULL
    ALTER TABLE dbo.ApiRequest
        ADD CONSTRAINT FK_ApiRequest_Dataset FOREIGN KEY (DatasetId) REFERENCES dbo.Dataset (Id);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ApiRequest_Dataset' AND object_id = OBJECT_ID('dbo.ApiRequest')
)
    CREATE INDEX IX_ApiRequest_Dataset ON dbo.ApiRequest (DatasetId);
GO

/* 4) Run.DatasetId: add nullable, backfill, then enforce NOT NULL + FK. */
IF COL_LENGTH('dbo.Run', 'DatasetId') IS NULL
    ALTER TABLE dbo.Run ADD DatasetId INT NULL;
GO

UPDATE rn
SET rn.DatasetId = d.Id
FROM dbo.Run rn
INNER JOIN dbo.Dataset d ON d.ProjectId = rn.ProjectId AND d.Name = N'Default'
WHERE rn.DatasetId IS NULL;
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Run') AND name = 'DatasetId' AND is_nullable = 1
)
    ALTER TABLE dbo.Run ALTER COLUMN DatasetId INT NOT NULL;
GO

IF OBJECT_ID('dbo.FK_Run_Dataset', 'F') IS NULL
    ALTER TABLE dbo.Run
        ADD CONSTRAINT FK_Run_Dataset FOREIGN KEY (DatasetId) REFERENCES dbo.Dataset (Id);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Run_Dataset' AND object_id = OBJECT_ID('dbo.Run')
)
    CREATE INDEX IX_Run_Dataset ON dbo.Run (DatasetId);
GO

COMMIT TRANSACTION;
GO
