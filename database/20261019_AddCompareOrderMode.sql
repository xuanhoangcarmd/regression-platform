USE RegressionDB;
GO

/*
  Compare order mode ("Ignore order" vs "Strict order")
  -----------------------------------------------------
  Until now a capture stored the response with object keys SORTED (canonical) and the compare
  engine matched array items by identity regardless of position. A compare can now opt into
  STRICT order: object key order and array element order count as differences.

  - RunResponse.ResponseOrderedHash         SHA-256 of the body in its ORIGINAL key order (new captures).
  - RunResponseOverride.ResponseOrderedHash same, for edited (expected) responses.
  - CompareJob.IgnoreOrder                  1 = ignore order (default — the existing behaviour), 0 = strict.

  Rows written before this migration keep ResponseOrderedHash = NULL; the engine falls back to
  ResponseHash for them (their stored body is the sorted canonical form, so both hashes agree).

  Additive, idempotent, metadata-only column adds (no table rebuild). Existing compares → IgnoreOrder = 1.
*/

IF COL_LENGTH('dbo.RunResponse', 'ResponseOrderedHash') IS NULL
    ALTER TABLE dbo.RunResponse ADD ResponseOrderedHash CHAR(64) NULL;
GO

IF COL_LENGTH('dbo.RunResponseOverride', 'ResponseOrderedHash') IS NULL
    ALTER TABLE dbo.RunResponseOverride ADD ResponseOrderedHash CHAR(64) NULL;
GO

IF COL_LENGTH('dbo.CompareJob', 'IgnoreOrder') IS NULL
    ALTER TABLE dbo.CompareJob
        ADD IgnoreOrder BIT NOT NULL CONSTRAINT DF_CompareJob_IgnoreOrder DEFAULT 1;
GO
