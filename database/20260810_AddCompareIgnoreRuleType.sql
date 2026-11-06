/* =============================================================================
   Migration: 20260810_AddCompareIgnoreRuleType
   Purpose  : Add a RuleType to CompareIgnoreRule so a rule can either IGNORE a
              field (existing behavior) or EXTRACT-ROOT (lift a subtree, e.g.
              $.data, to the response root before comparison). Extract-root lets
              an envelope-wrapped response (v20: {message,success,code,data:{…}})
              be compared against an un-wrapped one (v19: {…}).

     - ADD COLUMN CompareIgnoreRule.RuleType VARCHAR(20) NOT NULL DEFAULT 'Ignore'.
       Existing rows default to 'Ignore' → behavior is unchanged.
     - CHECK constraint restricts it to ('Ignore', 'ExtractRoot').

   Forward-only and idempotent (safe to rerun). Additive — no existing flow changes.
   ============================================================================= */

USE RegressionDB;
GO

/* 1) Column (defaults existing rows to 'Ignore' — the current behavior). */
IF COL_LENGTH('dbo.CompareIgnoreRule', 'RuleType') IS NULL
BEGIN
    ALTER TABLE dbo.CompareIgnoreRule
        ADD RuleType VARCHAR(20) NOT NULL
            CONSTRAINT DF_CompareIgnoreRule_RuleType DEFAULT ('Ignore');
END;
GO

/* 2) Restrict to the two known kinds. */
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_CompareIgnoreRule_RuleType'
      AND parent_object_id = OBJECT_ID('dbo.CompareIgnoreRule')
)
BEGIN
    ALTER TABLE dbo.CompareIgnoreRule
        ADD CONSTRAINT CK_CompareIgnoreRule_RuleType CHECK (RuleType IN ('Ignore', 'ExtractRoot'));
END;
GO
