/* =============================================================================
   Migration: 20261106_AddExpectedRunGeneration
   Spec     : specs/011-expected-run-generation
   Purpose  : Expected Run generation — a "Generated" run is built from a finished source run by
              applying an approved Change Spec (structured operations + matchers from tickets).
              Comparing the real new-sprint run against it leaves only unexpected differences.

     - ChangeSpec                       project-scoped spec (title, tickets, status, operations JSON).
     - Run.SourceRunId / ChangeSpecId   provenance of a Generated run (RunType = 'Generated').
     - Run.GenerationBaseline           which source bodies were used: 'Expected' (edits) / 'Original'.
     - RunResponse.ChangeOps            per generated response: JSON array of the operation indexes
                                        that touched it (NULL = untouched / not generated).
     - RunExpectation                   matcher-based expectations of a Generated run, evaluated by
                                        the compare when that run is Run A.
     - CompareResult.ExpectationFailedCount   count of the new per-request result 'ExpectationFailed'
                                        (CompareDetail.Result is varchar(20) without a CHECK).

   Forward-only and idempotent (safe to rerun). Additive — existing runs, compares and reports are
   unchanged (all new columns are NULL / default 0).
   ============================================================================= */

USE RegressionDB;
GO

/* 1) Change Spec */
IF OBJECT_ID('dbo.ChangeSpec', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChangeSpec
    (
        Id             INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_ChangeSpec PRIMARY KEY CLUSTERED,
        ProjectId      INT            NOT NULL,
        Title          NVARCHAR(200)  NOT NULL,
        -- Free-text ticket keys, comma separated (e.g. "IABB-419, IABB-371").
        TicketKeys     NVARCHAR(500)  NULL,
        Status         VARCHAR(20)    NOT NULL CONSTRAINT DF_ChangeSpec_Status DEFAULT 'Draft',
        -- Ordered operations (JSON array). Paths / values / matchers / conditions only — never secrets.
        OperationsJson NVARCHAR(MAX)  NOT NULL,
        CreatedDate    DATETIME2      NOT NULL CONSTRAINT DF_ChangeSpec_CreatedDate DEFAULT SYSUTCDATETIME(),
        UpdatedDate    DATETIME2      NULL,
        ApprovedDate   DATETIME2      NULL,
        CONSTRAINT FK_ChangeSpec_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Project (Id),
        CONSTRAINT CK_ChangeSpec_Status CHECK (Status IN ('Draft', 'Approved', 'Archived'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChangeSpec_Project' AND object_id = OBJECT_ID('dbo.ChangeSpec'))
    CREATE NONCLUSTERED INDEX IX_ChangeSpec_Project ON dbo.ChangeSpec (ProjectId, Status);
GO

/* 2) Run provenance */
IF COL_LENGTH('dbo.Run', 'SourceRunId') IS NULL
    ALTER TABLE dbo.Run ADD SourceRunId BIGINT NULL
        CONSTRAINT FK_Run_SourceRun FOREIGN KEY REFERENCES dbo.Run (Id);
GO

IF COL_LENGTH('dbo.Run', 'ChangeSpecId') IS NULL
    ALTER TABLE dbo.Run ADD ChangeSpecId INT NULL
        CONSTRAINT FK_Run_ChangeSpec FOREIGN KEY REFERENCES dbo.ChangeSpec (Id);
GO

IF COL_LENGTH('dbo.Run', 'GenerationBaseline') IS NULL
    ALTER TABLE dbo.Run ADD GenerationBaseline VARCHAR(20) NULL
        CONSTRAINT CK_Run_GenerationBaseline CHECK (GenerationBaseline IN ('Original', 'Expected'));
GO

-- "Which Generated runs came from this run?" (blocks deleting a source run).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Run_SourceRun' AND object_id = OBJECT_ID('dbo.Run'))
    CREATE NONCLUSTERED INDEX IX_Run_SourceRun ON dbo.Run (SourceRunId) WHERE SourceRunId IS NOT NULL;
GO

/* 3) Per-response provenance (metadata-only add on the large RunResponse table) */
IF COL_LENGTH('dbo.RunResponse', 'ChangeOps') IS NULL
    ALTER TABLE dbo.RunResponse ADD ChangeOps NVARCHAR(400) NULL;
GO

/* 4) Matcher-based expectations of a Generated run */
IF OBJECT_ID('dbo.RunExpectation', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RunExpectation
    (
        Id          BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_RunExpectation PRIMARY KEY CLUSTERED,
        RunId       BIGINT         NOT NULL,
        RequestId   BIGINT         NOT NULL,
        -- Concrete path in the generated body (e.g. $.MostLikelyFixes[0].FixParts[1].PartTerminologyName).
        JsonPath    NVARCHAR(500)  NOT NULL,
        -- Matcher (JSON): { "type": "oneOf", "values": [...] } etc.
        MatcherJson NVARCHAR(1000) NOT NULL,
        OpIndex     INT            NOT NULL,
        TicketKey   NVARCHAR(50)   NULL,
        CONSTRAINT FK_RunExpectation_Run FOREIGN KEY (RunId) REFERENCES dbo.Run (Id) ON DELETE CASCADE,
        CONSTRAINT FK_RunExpectation_Request FOREIGN KEY (RequestId) REFERENCES dbo.ApiRequest (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RunExpectation_Run_Request' AND object_id = OBJECT_ID('dbo.RunExpectation'))
    CREATE NONCLUSTERED INDEX IX_RunExpectation_Run_Request
        ON dbo.RunExpectation (RunId, RequestId)
        INCLUDE (JsonPath, MatcherJson, OpIndex, TicketKey);
GO

/* 5) Compare summary count for the new per-request result 'ExpectationFailed' */
IF COL_LENGTH('dbo.CompareResult', 'ExpectationFailedCount') IS NULL
    ALTER TABLE dbo.CompareResult ADD ExpectationFailedCount INT NOT NULL
        CONSTRAINT DF_CompareResult_ExpectationFailedCount DEFAULT 0;
GO
