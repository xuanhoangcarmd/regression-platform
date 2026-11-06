USE RegressionDB;
GO

/*
  Expected Responses (baseline editing)
  -------------------------------------
  An admin edits a run's captured responses ("what it SHOULD return"). A compare can then use the
  edited version for Run A instead of the original capture (CompareJob.BaselineMode = 'Expected').
  The original RunResponse row is never modified.

  Safe to re-run (every step is guarded). Existing compares keep BaselineMode = 'Original', so the
  current compare flow is unchanged.
*/

-- 1) Edited (expected) responses — one per (RunId, RequestId).
--    Keyed by (RunId, RequestId), NOT RunResponse.Id: "Re-run request" deletes + re-inserts the
--    RunResponse row, and the edit must survive that. Deleting a run cascades to its edits.
IF OBJECT_ID('dbo.RunResponseOverride', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RunResponseOverride
    (
        Id            BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_RunResponseOverride PRIMARY KEY CLUSTERED,
        RunId         BIGINT        NOT NULL,
        RequestId     BIGINT        NOT NULL,
        -- NULL = keep the original status code.
        StatusCode    INT           NULL,
        -- Canonical body (same canonicalization as RunResponse.ResponseJson).
        ResponseJson  NVARCHAR(MAX) NOT NULL,
        -- SHA-256 of the canonical body, computed exactly like RunResponse.ResponseHash so the compare
        -- engine's hash fast path keeps working on edited responses.
        ResponseHash  CHAR(64)      NOT NULL,
        Reason        NVARCHAR(500) NULL,
        CreatedBy     NVARCHAR(100) NULL,
        CreatedDate   DATETIME2     NOT NULL CONSTRAINT DF_RunResponseOverride_CreatedDate DEFAULT SYSUTCDATETIME(),
        UpdatedBy     NVARCHAR(100) NULL,
        UpdatedDate   DATETIME2     NULL,
        CONSTRAINT FK_RunResponseOverride_Run FOREIGN KEY (RunId) REFERENCES dbo.Run (Id) ON DELETE CASCADE,
        CONSTRAINT FK_RunResponseOverride_Request FOREIGN KEY (RequestId) REFERENCES dbo.ApiRequest (Id)
    );
END
GO

-- Lookup by (RunId, RequestId); INCLUDE covers the compare engine's summary query (no key lookup).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_RunResponseOverride_Run_Request' AND object_id = OBJECT_ID('dbo.RunResponseOverride'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_RunResponseOverride_Run_Request
        ON dbo.RunResponseOverride (RunId, RequestId)
        INCLUDE (ResponseHash, StatusCode);
GO

-- 2) Which body a compare uses for Run A. Existing rows → 'Original' (unchanged behaviour).
IF COL_LENGTH('dbo.CompareJob', 'BaselineMode') IS NULL
    ALTER TABLE dbo.CompareJob
        ADD BaselineMode VARCHAR(20) NOT NULL
            CONSTRAINT DF_CompareJob_BaselineMode DEFAULT 'Original'
            CONSTRAINT CK_CompareJob_BaselineMode CHECK (BaselineMode IN ('Original', 'Expected'));
GO

-- 3) Per request: was Run A's edited (expected) body used? Existing rows → 0.
IF COL_LENGTH('dbo.CompareDetail', 'IsEdited') IS NULL
    ALTER TABLE dbo.CompareDetail
        ADD IsEdited BIT NOT NULL CONSTRAINT DF_CompareDetail_IsEdited DEFAULT 0;
GO

-- 4) Performance: cover the compare engine's per-run summary query
--    (SELECT RequestId, ResponseHash, StatusCode, ErrorMessage WHERE RunId = @RunId)
--    so it no longer does one key lookup per row into the ~2 GB clustered RunResponse index.
--    Same key / uniqueness as before — only INCLUDE columns are added.
IF NOT EXISTS (
    SELECT 1
    FROM sys.index_columns ic
    JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID('dbo.RunResponse') AND i.name = 'UX_Run_Request'
      AND ic.is_included_column = 1 AND c.name = 'ResponseHash'
)
    CREATE UNIQUE NONCLUSTERED INDEX UX_Run_Request
        ON dbo.RunResponse (RunId, RequestId)
        INCLUDE (ResponseHash, StatusCode, ErrorMessage)
        WITH (DROP_EXISTING = ON);
GO

-- 5) Performance: compare detail list / API statistics read (RequestId, Result, IsEdited) by
--    CompareResultId — cover them to avoid key lookups into the clustered CompareDetail (DiffJson).
IF NOT EXISTS (
    SELECT 1
    FROM sys.index_columns ic
    JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID('dbo.CompareDetail') AND i.name = 'IX_CompareDetail_Result'
      AND ic.is_included_column = 1 AND c.name = 'IsEdited'
)
    CREATE NONCLUSTERED INDEX IX_CompareDetail_Result
        ON dbo.CompareDetail (CompareResultId)
        INCLUDE (RequestId, Result, IsEdited)
        WITH (DROP_EXISTING = ON);
GO
