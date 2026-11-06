/* =============================================================================
   Migration: 20260629_CreateDataLibrary
   Purpose  : Stand up the "Data Library" — a GLOBAL (not project-scoped) catalog of
              vehicle payloads/raw buffers that users search by YMME / DTC / MIL to
              pull a payload for testing.

     - VehicleProfile : one vehicle scenario (searchable info). YMME stored as NAMES
                        (no enum ids); fields are filled in incrementally.
     - Payload        : full payload(s) for a profile (VARBINARY). 1 profile → many.
     - RawBuffer      : per-segment raw buffers for a profile. 1 profile → many.
     - VehicleDtc     : DTC / OEM codes for a profile (so DTC/OEM-code search works).

   Notes:
     - Intentionally NOT project-scoped (no ProjectId) — this is a shared, top-level
       library used across all projects.
     - Tables start empty; data is populated in a later step (manual/import/generator/job).
     - A dedicated YMME-decode API is planned; for now YMME names come from a static file.

   Forward-only and idempotent.
   ============================================================================= */

USE RegressionDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

/* 1) VehicleProfile — the searchable parent (YMME as names + MIL + identity). */
IF OBJECT_ID('dbo.VehicleProfile', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleProfile
    (
        Id            BIGINT        NOT NULL IDENTITY(1, 1),
        -- Vehicle identity (YMME stored as names) --
        Vin           NVARCHAR(17)  NULL,
        [Year]        NVARCHAR(50)  NULL,
        Make          NVARCHAR(100) NULL,
        Model         NVARCHAR(100) NULL,
        Engine        NVARCHAR(100) NULL,
        Trim          NVARCHAR(100) NULL,
        Transmission  NVARCHAR(50)  NULL,
        UsbProductId  INT           NULL,          -- capture device USB Product ID (e.g. 1688)
        -- Diagnostic state (decoded snapshot) --
        Mil               BIT           NULL,          -- MIL on/off; NULL = unknown
        FreezeFrameDtc    NVARCHAR(20)  NULL,          -- FF_DTC, e.g. "P0420"
        LedStatus         NVARCHAR(20)  NULL,          -- Red / Yellow / Green
        Sil               NVARCHAR(10)  NULL,          -- ON / OFF
        AbsLight          NVARCHAR(10)  NULL,          -- ON / OFF
        TpmsLight         NVARCHAR(10)  NULL,          -- ON / OFF
        BatteryCondition  NVARCHAR(50)  NULL,          -- e.g. "Battery Good"
        BatteryVoltage    NVARCHAR(20)  NULL,          -- e.g. "11.65 V"
        OdometerValue     INT           NULL,
        OdometerUnit      NVARCHAR(10)  NULL,          -- miles / km
        RecordTimeJson    NVARCHAR(MAX) NULL,          -- RecordTime object as JSON (per-segment ms)
        -- Housekeeping --
        Notes         NVARCHAR(500) NULL,
        IsActive      BIT           NOT NULL CONSTRAINT DF_VehicleProfile_IsActive DEFAULT (1),
        CreatedDate   DATETIME2     NOT NULL CONSTRAINT DF_VehicleProfile_CreatedDate DEFAULT (getutcdate()),
        UpdatedDate   DATETIME2     NULL,
        CONSTRAINT PK_VehicleProfile PRIMARY KEY (Id)
    );
END;
GO

-- Search indexes: by VIN, and by YMME.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VehicleProfile_Vin' AND object_id = OBJECT_ID('dbo.VehicleProfile'))
    CREATE INDEX IX_VehicleProfile_Vin ON dbo.VehicleProfile (Vin);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VehicleProfile_Ymme' AND object_id = OBJECT_ID('dbo.VehicleProfile'))
    CREATE INDEX IX_VehicleProfile_Ymme ON dbo.VehicleProfile (Make, Model, [Year], Engine);
GO

/* 2) Payload — full payload(s) for a profile. */
IF OBJECT_ID('dbo.Payload', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payload
    (
        Id               BIGINT         NOT NULL IDENTITY(1, 1),
        VehicleProfileId BIGINT         NOT NULL,
        Name             NVARCHAR(200)  NULL,
        PayloadData      VARBINARY(MAX) NOT NULL,  -- GZip-compressed payload bytes (compressed in the app)
        Compression      NVARCHAR(10)   NOT NULL CONSTRAINT DF_Payload_Compression DEFAULT ('gzip'),  -- gzip / none
        ByteSize         INT            NULL,       -- original (uncompressed) size in bytes
        Source           NVARCHAR(50)   NULL,   -- e.g. Generated / Imported
        CreatedDate      DATETIME2      NOT NULL CONSTRAINT DF_Payload_CreatedDate DEFAULT (getutcdate()),
        CONSTRAINT PK_Payload PRIMARY KEY (Id),
        CONSTRAINT FK_Payload_VehicleProfile FOREIGN KEY (VehicleProfileId) REFERENCES dbo.VehicleProfile (Id) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payload_VehicleProfile' AND object_id = OBJECT_ID('dbo.Payload'))
    CREATE INDEX IX_Payload_VehicleProfile ON dbo.Payload (VehicleProfileId);
GO

/* 3) RawBuffer — per-segment raw buffers for a profile. */
IF OBJECT_ID('dbo.RawBuffer', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RawBuffer
    (
        Id               BIGINT         NOT NULL IDENTITY(1, 1),
        VehicleProfileId BIGINT         NOT NULL,
        BufferType       NVARCHAR(50)   NOT NULL,  -- VinProfileRaw, MonitorStatusEcmRaw, FreezeFrameEcmRaw, DtcsEcmRaw, OemModuleRaw, ...
        SegmentIndex     INT            NULL,       -- ordering within a list (e.g. OemModule blocks)
        RawData          VARBINARY(MAX) NOT NULL,  -- GZip-compressed raw bytes (compressed in the app)
        Compression      NVARCHAR(10)   NOT NULL CONSTRAINT DF_RawBuffer_Compression DEFAULT ('gzip'),  -- gzip / none
        ByteSize         INT            NULL,       -- original (uncompressed) size in bytes
        CreatedDate      DATETIME2      NOT NULL CONSTRAINT DF_RawBuffer_CreatedDate DEFAULT (getutcdate()),
        CONSTRAINT PK_RawBuffer PRIMARY KEY (Id),
        CONSTRAINT FK_RawBuffer_VehicleProfile FOREIGN KEY (VehicleProfileId) REFERENCES dbo.VehicleProfile (Id) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RawBuffer_VehicleProfile' AND object_id = OBJECT_ID('dbo.RawBuffer'))
    CREATE INDEX IX_RawBuffer_VehicleProfile ON dbo.RawBuffer (VehicleProfileId);
GO

/* 4) VehicleDtc — DTC / OEM codes for a profile (powers DTC/OEM-code search). */
IF OBJECT_ID('dbo.VehicleDtc', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleDtc
    (
        Id               BIGINT         NOT NULL IDENTITY(1, 1),
        VehicleProfileId BIGINT         NOT NULL,
        Source           NVARCHAR(20)   NOT NULL,  -- ECU / TCU / OEM
        Code             NVARCHAR(50)   NOT NULL,  -- e.g. P0420, C1101 (OEM code)
        StatusType       NVARCHAR(100)  NULL,      -- e.g. Stored, Current/MIL
        [System]         NVARCHAR(100)  NULL,
        SubSystem        NVARCHAR(100)  NULL,
        Definition       NVARCHAR(500)  NULL,
        CreatedDate      DATETIME2      NOT NULL CONSTRAINT DF_VehicleDtc_CreatedDate DEFAULT (getutcdate()),
        CONSTRAINT PK_VehicleDtc PRIMARY KEY (Id),
        CONSTRAINT FK_VehicleDtc_VehicleProfile FOREIGN KEY (VehicleProfileId) REFERENCES dbo.VehicleProfile (Id) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VehicleDtc_VehicleProfile' AND object_id = OBJECT_ID('dbo.VehicleDtc'))
    CREATE INDEX IX_VehicleDtc_VehicleProfile ON dbo.VehicleDtc (VehicleProfileId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VehicleDtc_Code' AND object_id = OBJECT_ID('dbo.VehicleDtc'))
    CREATE INDEX IX_VehicleDtc_Code ON dbo.VehicleDtc (Code);
GO

/* 5) VehicleMonitor — ECU/TCU readiness monitors for a profile. 1 profile → many. */
IF OBJECT_ID('dbo.VehicleMonitor', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleMonitor
    (
        Id               BIGINT        NOT NULL IDENTITY(1, 1),
        VehicleProfileId BIGINT        NOT NULL,
        Source           NVARCHAR(10)  NOT NULL,  -- ECU / TCU
        Name             NVARCHAR(150) NOT NULL,  -- e.g. "Catalyst Monitoring"
        [Key]            INT           NOT NULL,  -- 1=ON(MIL), 2=Not Supported, 3=Not Complete, 4=Complete/Ready
        CreatedDate      DATETIME2     NOT NULL CONSTRAINT DF_VehicleMonitor_CreatedDate DEFAULT (getutcdate()),
        CONSTRAINT PK_VehicleMonitor PRIMARY KEY (Id),
        CONSTRAINT FK_VehicleMonitor_VehicleProfile FOREIGN KEY (VehicleProfileId) REFERENCES dbo.VehicleProfile (Id) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VehicleMonitor_VehicleProfile' AND object_id = OBJECT_ID('dbo.VehicleMonitor'))
    CREATE INDEX IX_VehicleMonitor_VehicleProfile ON dbo.VehicleMonitor (VehicleProfileId);
GO

/* 6) VehicleFreezeFrame — ECU/TCU freeze-frame items for a profile. 1 profile → many. */
IF OBJECT_ID('dbo.VehicleFreezeFrame', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleFreezeFrame
    (
        Id               BIGINT        NOT NULL IDENTITY(1, 1),
        VehicleProfileId BIGINT        NOT NULL,
        Source           NVARCHAR(10)  NOT NULL,  -- ECU / TCU
        Name             NVARCHAR(200) NOT NULL,
        Unit             NVARCHAR(50)  NULL,
        Pid              INT           NULL,
        ValuesJson       NVARCHAR(MAX) NULL,       -- item values (list) as JSON
        CreatedDate      DATETIME2     NOT NULL CONSTRAINT DF_VehicleFreezeFrame_CreatedDate DEFAULT (getutcdate()),
        CONSTRAINT PK_VehicleFreezeFrame PRIMARY KEY (Id),
        CONSTRAINT FK_VehicleFreezeFrame_VehicleProfile FOREIGN KEY (VehicleProfileId) REFERENCES dbo.VehicleProfile (Id) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VehicleFreezeFrame_VehicleProfile' AND object_id = OBJECT_ID('dbo.VehicleFreezeFrame'))
    CREATE INDEX IX_VehicleFreezeFrame_VehicleProfile ON dbo.VehicleFreezeFrame (VehicleProfileId);
GO

COMMIT TRANSACTION;
GO
