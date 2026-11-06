/* =============================================================================
   Migration: 20260702_CreateReportAiAnalysis
   Purpose  : Persist the AI analysis of a report (AI Analysis tab) so it isn't
              re-billed to the LLM on every visit.

     - ReportAiAnalysis : one stored analysis per report (UPSERT keyed by ReportId).
                          The provider output is stored as JSON (ContentJson); the
                          provider name + generation time are columns for display.

   Notes:
     - One analysis per report — enforced by UQ_ReportAiAnalysis_Report.
     - Regenerating (the "Regenerate" button) re-runs the provider and UPSERTs in
       place, bumping GeneratedDate.
     - The application also keeps a short (~15 min) in-memory cache in front of this.

   Forward-only and idempotent.
   ============================================================================= */

USE RegressionDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

/* ReportAiAnalysis — one stored AI analysis per report. */
IF OBJECT_ID('dbo.ReportAiAnalysis', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportAiAnalysis
    (
        Id            BIGINT         NOT NULL IDENTITY(1, 1),
        ReportId      BIGINT         NOT NULL,                  -- the report this analysis covers
        Provider      VARCHAR(30)    NOT NULL,                  -- e.g. DeepSeek / RuleBased
        ContentJson   NVARCHAR(MAX)  NOT NULL,                  -- serialized ReportAiAnalysisResult
        GeneratedDate DATETIME2      NOT NULL
            CONSTRAINT DF_ReportAiAnalysis_GeneratedDate DEFAULT (getutcdate()),
        CreatedDate   DATETIME2      NOT NULL
            CONSTRAINT DF_ReportAiAnalysis_CreatedDate DEFAULT (getutcdate()),

        CONSTRAINT PK_ReportAiAnalysis PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ReportAiAnalysis_Report FOREIGN KEY (ReportId) REFERENCES dbo.Report (Id)
    );

    -- One analysis per report (also the UPSERT key).
    CREATE UNIQUE INDEX UQ_ReportAiAnalysis_Report ON dbo.ReportAiAnalysis (ReportId);
END;
GO

COMMIT TRANSACTION;
GO
