/* =============================================================================
   Migration: 20261020_AddIgnoreRuleTrimAfter
   Purpose  : A third compare-rule kind, 'TrimAfter': for the string value(s) at JsonPath, compare
              only the text BEFORE the first occurrence of Delimiter. E.g. a signed URL
                https://host/path/img.jpg?Expires=…&Signature=…
              with Delimiter '?' compares as https://host/path/img.jpg (the per-call query differs).

     - ADD COLUMN CompareIgnoreRule.Delimiter NVARCHAR(50) NULL (required only for 'TrimAfter').
     - Widen CK_CompareIgnoreRule_RuleType to ('Ignore', 'ExtractRoot', 'TrimAfter').

   Forward-only and idempotent (safe to rerun). Additive — existing rules are unchanged.
   ============================================================================= */

USE RegressionDB;
GO

IF COL_LENGTH('dbo.CompareIgnoreRule', 'Delimiter') IS NULL
BEGIN
    ALTER TABLE dbo.CompareIgnoreRule ADD Delimiter NVARCHAR(50) NULL;
END;
GO

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_CompareIgnoreRule_RuleType'
      AND parent_object_id = OBJECT_ID('dbo.CompareIgnoreRule')
      AND definition NOT LIKE '%TrimAfter%'
)
BEGIN
    ALTER TABLE dbo.CompareIgnoreRule DROP CONSTRAINT CK_CompareIgnoreRule_RuleType;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_CompareIgnoreRule_RuleType'
      AND parent_object_id = OBJECT_ID('dbo.CompareIgnoreRule')
)
BEGIN
    ALTER TABLE dbo.CompareIgnoreRule
        ADD CONSTRAINT CK_CompareIgnoreRule_RuleType CHECK (RuleType IN ('Ignore', 'ExtractRoot', 'TrimAfter'));
END;
GO
