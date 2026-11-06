USE RegressionDB;
GO

-- Soft-delete marker for pooled requests. A request that has been run (has RunResponse rows) is marked
-- IsDeleted = 1 instead of being hard-deleted, so its run/compare history is preserved; requests that
-- were never run are hard-deleted outright. Soft-deleted requests are hidden from the pool list and are
-- never executed, but their captured responses stay visible in run/compare views.
IF COL_LENGTH('dbo.ApiRequest', 'IsDeleted') IS NULL
    ALTER TABLE dbo.ApiRequest
        ADD IsDeleted BIT NOT NULL CONSTRAINT DF_ApiRequest_IsDeleted DEFAULT 0;
GO
