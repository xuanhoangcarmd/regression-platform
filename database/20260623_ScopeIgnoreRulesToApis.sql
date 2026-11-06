/* =============================================================================
   Migration: 20260623_ScopeIgnoreRulesToApis
   Purpose  : Scope ignore rules to APIs, and drop the unrelated IgnoreOrder flag.

     - DROP COLUMN CompareIgnoreRule.IgnoreOrder (does not belong to ignore rules).
     - ADD TABLE CompareIgnoreRuleApi: many-to-many between a rule and the APIs it
       applies to. No link rows for a rule  ==>  the rule applies to ALL APIs.

   Forward-only and idempotent. DROP COLUMN is destructive (requested explicitly).
   ============================================================================= */

USE RegressionDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

/* 1) Drop IgnoreOrder (drop its DEFAULT constraint first, name is system-generated). */
IF COL_LENGTH('dbo.CompareIgnoreRule', 'IgnoreOrder') IS NOT NULL
BEGIN
    DECLARE @defaultConstraint sysname;
    SELECT @defaultConstraint = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.CompareIgnoreRule') AND c.name = 'IgnoreOrder';

    IF @defaultConstraint IS NOT NULL
        EXEC('ALTER TABLE dbo.CompareIgnoreRule DROP CONSTRAINT ' + @defaultConstraint);

    ALTER TABLE dbo.CompareIgnoreRule DROP COLUMN IgnoreOrder;
END;

/* 2) Junction table: which APIs a rule applies to (empty = all APIs). */
IF OBJECT_ID('dbo.CompareIgnoreRuleApi', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompareIgnoreRuleApi
    (
        Id           INT NOT NULL IDENTITY(1, 1),
        IgnoreRuleId INT NOT NULL,
        ApiId        INT NOT NULL,
        CONSTRAINT PK_CompareIgnoreRuleApi PRIMARY KEY (Id),
        CONSTRAINT FK_CompareIgnoreRuleApi_CompareIgnoreRule
            FOREIGN KEY (IgnoreRuleId) REFERENCES dbo.CompareIgnoreRule (Id) ON DELETE CASCADE,
        CONSTRAINT FK_CompareIgnoreRuleApi_Api
            FOREIGN KEY (ApiId) REFERENCES dbo.Api (Id) ON DELETE CASCADE,
        CONSTRAINT UQ_CompareIgnoreRuleApi_Rule_Api UNIQUE (IgnoreRuleId, ApiId)
    );
END;

COMMIT TRANSACTION;
GO
