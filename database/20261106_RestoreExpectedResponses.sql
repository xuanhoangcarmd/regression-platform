/* =============================================================================
   Migration: 20261106_RestoreExpectedResponses
   Purpose  : Re-create the Expected Responses feature (edited "expected" responses + compare baseline
              mode) after the FIRST version of 20261106_RollbackExpectedFeatures.sql removed it by
              mistake. Equivalent to 20261007_AddExpectedResponses.sql plus the
              RunResponseOverride.ResponseOrderedHash column from 20261019_AddCompareOrderMode.sql
              (the current code reads/writes it).

   Note: edited responses saved before the rollback were deleted with the table and cannot be
   recovered — re-enter them on /projects/<code>/runs/<id>/expected.
   Existing compares get BaselineMode = 'Original' and IsEdited = 0 (exactly what they used).

   Idempotent (every step is guarded) — safe to rerun, and a no-op where nothing was removed.
   ============================================================================= */

USE RegressionDB;
GO

/* 1) Edited (expected) responses — one per (RunId, RequestId). Deleting a run cascades to its edits. */
IF OBJECT_ID('dbo.RunResponseOverride', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RunResponseOverride
    (
        Id                  BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_RunResponseOverride PRIMARY KEY CLUSTERED,
        RunId               BIGINT        NOT NULL,
        RequestId           BIGINT        NOT NULL,
        -- NULL = keep the original status code.
        StatusCode          INT           NULL,
        -- Body in the editor's key order (compact).
        ResponseJson        NVARCHAR(MAX) NOT NULL,
        -- SHA-256 of the key-sorted canonical body (same as RunResponse.ResponseHash) — compare fast path.
        ResponseHash        CHAR(64)      NOT NULL,
        -- SHA-256 of the original-order body — strict-order compares.
        ResponseOrderedHash CHAR(64)      NULL,
        Reason              NVARCHAR(500) NULL,
        CreatedBy           NVARCHAR(100) NULL,
        CreatedDate         DATETIME2     NOT NULL CONSTRAINT DF_RunResponseOverride_CreatedDate DEFAULT SYSUTCDATETIME(),
        UpdatedBy           NVARCHAR(100) NULL,
        UpdatedDate         DATETIME2     NULL,
        CONSTRAINT FK_RunResponseOverride_Run FOREIGN KEY (RunId) REFERENCES dbo.Run (Id) ON DELETE CASCADE,
        CONSTRAINT FK_RunResponseOverride_Request FOREIGN KEY (RequestId) REFERENCES dbo.ApiRequest (Id)
    );
END;
GO

-- Table kept from before (only the order-mode column missing).
IF COL_LENGTH('dbo.RunResponseOverride', 'ResponseOrderedHash') IS NULL
    ALTER TABLE dbo.RunResponseOverride ADD ResponseOrderedHash CHAR(64) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_RunResponseOverride_Run_Request' AND object_id = OBJECT_ID('dbo.RunResponseOverride'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_RunResponseOverride_Run_Request
        ON dbo.RunResponseOverride (RunId, RequestId)
        INCLUDE (ResponseHash, StatusCode);
GO

/* 2) Which bodies a compare uses: 'Original' (captured) or 'Expected' (edits where present). */
IF COL_LENGTH('dbo.CompareJob', 'BaselineMode') IS NULL
    ALTER TABLE dbo.CompareJob
        ADD BaselineMode VARCHAR(20) NOT NULL
            CONSTRAINT DF_CompareJob_BaselineMode DEFAULT 'Original'
            CONSTRAINT CK_CompareJob_BaselineMode CHECK (BaselineMode IN ('Original', 'Expected'));
GO

/* 3) Per request: was an edited (expected) response used? Existing rows → 0. */
IF COL_LENGTH('dbo.CompareDetail', 'IsEdited') IS NULL
    ALTER TABLE dbo.CompareDetail
        ADD IsEdited BIT NOT NULL CONSTRAINT DF_CompareDetail_IsEdited DEFAULT 0;
GO

/* 4) Cover the compare detail list again (RequestId, Result, IsEdited) — the rollback rebuilt this
      index without IsEdited. Same key; only INCLUDE columns change. */
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
