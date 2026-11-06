/* =============================================================================
   Migration: 20260713_AddRunParallelism
   Purpose  : Let a run execute in parallel. Two independent knobs:
                MaxParallelApis           - how many APIs run concurrently
                MaxParallelRequestsPerApi - within an API, how many requests concurrently
              Effective concurrency = MaxParallelApis * MaxParallelRequestsPerApi
              (the app caps the product at 20). Default 1/1 = sequential (unchanged behavior).

   Forward-only and idempotent.
   ============================================================================= */

USE RegressionDB;
GO

IF COL_LENGTH('dbo.Run', 'MaxParallelApis') IS NULL
    ALTER TABLE dbo.Run ADD MaxParallelApis INT NOT NULL
        CONSTRAINT DF_Run_MaxParallelApis DEFAULT 1;
GO

IF COL_LENGTH('dbo.Run', 'MaxParallelRequestsPerApi') IS NULL
    ALTER TABLE dbo.Run ADD MaxParallelRequestsPerApi INT NOT NULL
        CONSTRAINT DF_Run_MaxParallelRequestsPerApi DEFAULT 1;
GO
