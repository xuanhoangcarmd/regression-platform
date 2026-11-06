/* =============================================================================
   Migration: 20260623_Create_ProjectConfiguration
   Purpose  : Store environment configuration (Base URL + request headers) per Project.

   Adds three tables (additive, backward-compatible — no existing table is altered):
     Project                      - minimal project identity (first concrete step of the
                                     planned multi-project feature; scopes configuration only,
                                     Api/ApiRequest are NOT project-scoped yet).
     ProjectConfiguration         - one row per (Project, Environment): the Base URL.
     ProjectConfigurationHeader   - the headers sent with every request in that environment.

   Conventions match the current schema: Id INT IDENTITY PK, CreatedDate (UTC default) +
   nullable UpdatedDate + IsActive bit (no CreatedBy/UpdatedBy — full audit is still planned),
   explicit named constraints, varchar for short enum-like codes, nvarchar for text.

   Secrets: secret header values are NEVER stored in plaintext (docs/rules/database/security.md).
   A non-secret value goes in HeaderValue; a secret value goes encrypted in SecretValueEncrypted
   (AES, same key mechanism planned for response encryption). A CHECK enforces this split.

   Forward-only and idempotent (guards with IF OBJECT_ID / IF NOT EXISTS).
   ============================================================================= */

SET XACT_ABORT ON;
BEGIN TRANSACTION;

/* ----------------------------------------------------------------------------
   1) Project
   ---------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Project', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Project
    (
        Id          INT            NOT NULL IDENTITY(1, 1),
        Code        NVARCHAR(50)   NOT NULL,   -- route/slug, e.g. 'oreilly'
        Name        NVARCHAR(200)  NOT NULL,
        Description NVARCHAR(1000) NULL,
        IsActive    BIT            NOT NULL CONSTRAINT DF_Project_IsActive    DEFAULT (1),
        CreatedDate DATETIME2      NOT NULL CONSTRAINT DF_Project_CreatedDate DEFAULT (GETUTCDATE()),
        UpdatedDate DATETIME2      NULL,
        CONSTRAINT PK_Project        PRIMARY KEY (Id),
        CONSTRAINT UQ_Project_Code   UNIQUE (Code)
    );
END;
GO

/* ----------------------------------------------------------------------------
   2) ProjectConfiguration  (one per Project + Environment)
   ---------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.ProjectConfiguration', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectConfiguration
    (
        Id          INT           NOT NULL IDENTITY(1, 1),
        ProjectId   INT           NOT NULL,
        Environment VARCHAR(20)   NOT NULL,   -- 'DEV' | 'STG' | 'LIVE'
        BaseUrl     NVARCHAR(500) NULL,
        IsActive    BIT           NOT NULL CONSTRAINT DF_ProjectConfiguration_IsActive    DEFAULT (1),
        CreatedDate DATETIME2     NOT NULL CONSTRAINT DF_ProjectConfiguration_CreatedDate DEFAULT (GETUTCDATE()),
        UpdatedDate DATETIME2     NULL,
        CONSTRAINT PK_ProjectConfiguration PRIMARY KEY (Id),
        CONSTRAINT FK_ProjectConfiguration_Project
            FOREIGN KEY (ProjectId) REFERENCES dbo.Project (Id) ON DELETE CASCADE,
        CONSTRAINT CK_ProjectConfiguration_Environment
            CHECK (Environment IN ('DEV', 'STG', 'LIVE')),
        -- One configuration row per environment within a project.
        CONSTRAINT UQ_ProjectConfiguration_Project_Environment UNIQUE (ProjectId, Environment)
    );
END;
GO

/* ----------------------------------------------------------------------------
   3) ProjectConfigurationHeader  (headers for a configuration)
   ---------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.ProjectConfigurationHeader', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectConfigurationHeader
    (
        Id                   INT            NOT NULL IDENTITY(1, 1),
        ConfigurationId      INT            NOT NULL,
        HeaderKey            NVARCHAR(100)  NOT NULL,
        HeaderValue          NVARCHAR(2000) NULL,           -- plaintext (non-secret only)
        SecretValueEncrypted VARBINARY(MAX) NULL,           -- AES ciphertext (secret only)
        IsSecret             BIT            NOT NULL CONSTRAINT DF_ProjectConfigurationHeader_IsSecret    DEFAULT (0),
        CreatedDate          DATETIME2      NOT NULL CONSTRAINT DF_ProjectConfigurationHeader_CreatedDate DEFAULT (GETUTCDATE()),
        UpdatedDate          DATETIME2      NULL,
        CONSTRAINT PK_ProjectConfigurationHeader PRIMARY KEY (Id),
        CONSTRAINT FK_ProjectConfigurationHeader_ProjectConfiguration
            FOREIGN KEY (ConfigurationId) REFERENCES dbo.ProjectConfiguration (Id) ON DELETE CASCADE,
        -- A secret keeps no plaintext; a non-secret keeps no ciphertext. Never store a secret in plaintext.
        CONSTRAINT CK_ProjectConfigurationHeader_SecretStorage
            CHECK (
                (IsSecret = 1 AND HeaderValue IS NULL)
             OR (IsSecret = 0 AND SecretValueEncrypted IS NULL)
            ),
        -- Header keys are unique within a configuration.
        CONSTRAINT UQ_ProjectConfigurationHeader_Configuration_HeaderKey UNIQUE (ConfigurationId, HeaderKey)
    );
END;
GO

/* ----------------------------------------------------------------------------
   4) Seed the known projects (reference data) so the existing UI routes resolve.
      Idempotent: only inserts a Code that is not already present.
   ---------------------------------------------------------------------------- */
INSERT INTO dbo.Project (Code, Name, Description)
SELECT s.Code, s.Name, s.Description
FROM (VALUES
    (N'oreilly',  N'O''Reilly',         N'O''Reilly Auto Parts'),
    (N'autozone', N'AutoZone',          N'AutoZone'),
    (N'carid',    N'CARiD',             N'CARiD'),
    (N'internal', N'Internal Services', N'Internal shared services')
) AS s (Code, Name, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Project p WHERE p.Code = s.Code);
GO

COMMIT TRANSACTION;
GO
