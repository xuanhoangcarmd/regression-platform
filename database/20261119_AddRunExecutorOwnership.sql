/* =============================================================================
   Migration: 20261119_AddRunExecutorOwnership
   Purpose  : Several API instances (e.g. a developer's local API and the deployed server) can share
              one RegressionDB. Each has its own in-memory run queue, so without ownership:
                - an instance's startup recovery marked EVERY 'Running' run as Failed, including runs
                  another live instance was still executing (run 523);
                - a run re-queued by one instance kept being executed by the other, inserting the
                  same request twice (UX_Run_Request violation, run 518).

     - Run.ExecutorInstance   which API process owns the run ("<node>|<pid>|<id>"): set when the run
                              is queued (Start / Re-run) and when an executor claims it (Running).
     - Run.HeartbeatAt        refreshed by the owning executor every ~20 s while Running. Recovery
                              only fails a Running run whose heartbeat is stale (> 2 min).

   Forward-only and idempotent (safe to rerun). Additive — existing rows keep NULL, which the code
   treats as "legacy" (never auto-failed by recovery; stop it manually if it is stuck).
   Run this BEFORE deploying the code that uses the columns, and on every shared DB once.
   ============================================================================= */

USE RegressionDB;
GO

IF COL_LENGTH('dbo.Run', 'ExecutorInstance') IS NULL
    ALTER TABLE dbo.Run ADD ExecutorInstance NVARCHAR(200) NULL;
GO

IF COL_LENGTH('dbo.Run', 'HeartbeatAt') IS NULL
    ALTER TABLE dbo.Run ADD HeartbeatAt DATETIME2(3) NULL;
GO
