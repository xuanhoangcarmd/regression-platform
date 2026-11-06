/* =============================================================================
   Migration: 20260701_CreateReport
   Purpose  : Stand up the "Report" feature — a point-in-time regression report for a
              single, Completed compare (1 Report = 1 Compare).

     - Report : project-scoped snapshot of a compare's headline stats + a computed
                RegressionScore and RiskLevel. Generated on demand ("Generate Report")
                and re-generated in place (UPSERT keyed by CompareJobId).

   Notes:
     - Snapshot semantics: counts/score/risk are stored at generation time (compare
       results are immutable once Completed). Regenerating bumps GeneratedDate.
     - One report per compare — enforced by UQ_Report_CompareJob.
     - Score/Risk defaults (tunable later, computed in the application layer):
         RegressionScore = round(UnchangedCount / max(TotalRequest,1) * 100, 1)
         Breaking API    = an API with >= 1 diff of ChangeType FieldRemoved | TypeChanged
         RiskLevel       = 0 breaking -> Low, 1-2 -> Medium, 3-5 -> High, >5 -> Critical
     - AI summary / export / email / share are LATER phases (not in this migration).

   Forward-only and idempotent.
   ============================================================================= */

USE RegressionDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

/* Report — one generated report per compare (1 Report = 1 Compare). */
IF OBJECT_ID('dbo.Report', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Report
    (
        Id               BIGINT        NOT NULL IDENTITY(1, 1),
        ProjectId        INT           NOT NULL,                 -- scoping (matches CompareJob.ProjectId)
        CompareJobId     BIGINT        NOT NULL,                 -- the compare this report covers
        CompareResultId  BIGINT        NOT NULL,                 -- snapshot source (finished result)
        Name             NVARCHAR(200) NOT NULL,                 -- e.g. "Regression Report — {RunA} vs {RunB}"
        Status           VARCHAR(20)   NOT NULL
            CONSTRAINT DF_Report_Status DEFAULT ('Completed'),   -- future: Pending/Generating for async AI
        -- Computed headline metrics --
        RegressionScore  DECIMAL(5, 2) NOT NULL,                 -- 0..100
        RiskLevel        VARCHAR(20)   NOT NULL,                 -- Low / Medium / High / Critical
        -- Snapshot of the compare's result counts --
        TotalRequest     INT           NOT NULL,
        UnchangedCount   INT           NOT NULL,
        ChangedCount     INT           NOT NULL,
        AddedCount       INT           NOT NULL,
        RemovedCount     INT           NOT NULL,
        FailedCount      INT           NOT NULL,
        TotalApis        INT           NOT NULL,
        BreakingApiCount INT           NOT NULL,
        -- Housekeeping --
        GeneratedDate    DATETIME2     NOT NULL
            CONSTRAINT DF_Report_GeneratedDate DEFAULT (getutcdate()),
        CreatedDate      DATETIME2     NOT NULL
            CONSTRAINT DF_Report_CreatedDate DEFAULT (getutcdate()),

        CONSTRAINT PK_Report PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Report_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Project (Id),
        CONSTRAINT FK_Report_CompareJob FOREIGN KEY (CompareJobId) REFERENCES dbo.CompareJob (Id),
        CONSTRAINT FK_Report_CompareResult FOREIGN KEY (CompareResultId) REFERENCES dbo.CompareResult (Id),
        CONSTRAINT CK_Report_RiskLevel CHECK (RiskLevel IN ('Low', 'Medium', 'High', 'Critical'))
    );

    -- One report per compare (also the UPSERT key).
    CREATE UNIQUE INDEX UQ_Report_CompareJob ON dbo.Report (CompareJobId);

    -- List query filters by project, newest first.
    CREATE INDEX IX_Report_Project ON dbo.Report (ProjectId);
END;
GO

COMMIT TRANSACTION;
GO
