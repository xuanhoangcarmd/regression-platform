/* =============================================================================
   Backfill: 20260828_BackfillRunResponseHash
   Purpose  : Recompute RunResponse.ResponseHash from the stored FULL body so it is
              a pure identity hash again.

   Why      : Historically the hash was derived from the IGNORE-APPLIED body (ignore
              rules baked in at capture time). The compare engine's fast path treats
              ResponseHash as "are these two bodies identical?" — so when an ignore
              rule was later disabled/removed, two responses that differ only in that
              (then-ignored) field still hashed equal and were wrongly classified
              Unchanged. The code now stores the full-body hash; this fixes existing
              rows the same way.

   Effect   : ResponseHash becomes a deterministic hash of the stored ResponseJson
              (the full canonical body). Identical bodies -> identical hash; any real
              byte difference -> different hash -> the compare deep-path re-applies the
              CURRENT ignore/extract rules and classifies correctly.

   NOTE     : Compare results are snapshots — after this backfill, RE-RUN (recreate)
              any compare whose classification you want corrected. Existing
              CompareDetail rows are NOT recomputed by this script.

   Idempotent: safe to rerun. Requires SQL Server 2016+ (HASHBYTES over NVARCHAR(MAX)).
   ============================================================================= */

USE RegressionDB;
GO

UPDATE RunResponse
SET ResponseHash = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', ResponseJson), 2))
WHERE ResponseJson IS NOT NULL
  AND ResponseHash <> LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', ResponseJson), 2));
GO
