/* =============================================================================
   Migration: 20260630_RebuildDataLibrary
   Purpose  : Restructure the Data Library. DESTRUCTIVE — drops all Data Library
              tables (wiping data) and recreates them with the finalized schema:

     - VehicleProfile: renamed/retyped columns to match the decode response, plus
       3 boolean "Has…" flags; FreezeFrameDtc / OdometerUnit / RecordTimeJson removed.
         Engine            -> EngineType
         Mil (bit)         -> ToolMilStatus (int; 0=Unknown, 1=On, 3=Off)
         LedStatus         -> ToolLEDStatus (int)
         AbsLight          -> ABSLightStatus
         TpmsLight         -> TPMSLightStatus
         BatteryCondition  -> BatteryConditionStatus
         OdometerValue     -> Odometer (int; NA -> NULL)
         MilDtc            -> PrimaryDtc
         + HasRecordTime, HasFreezeFrames, HasMonitors
     - Payload / RawBuffer / VehicleDtc: recreated unchanged (kept).
     - VehicleFreezeFrame / VehicleMonitor: REMOVED for good (replaced by Has flags).
     - DataLibraryImportState: cleared so the import restarts from newest.

   Run AFTER the earlier Data Library migrations. Re-import to repopulate.
   ============================================================================= */

USE RegressionDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

/* Drop in FK order (children first). */
DROP TABLE IF EXISTS dbo.VehicleFreezeFrame;
DROP TABLE IF EXISTS dbo.VehicleMonitor;
DROP TABLE IF EXISTS dbo.VehicleDtc;
DROP TABLE IF EXISTS dbo.RawBuffer;
DROP TABLE IF EXISTS dbo.Payload;
DROP TABLE IF EXISTS dbo.VehicleProfile;
GO

/* VehicleProfile — searchable snapshot (finalized columns). */
CREATE TABLE dbo.VehicleProfile
(
    Id                     BIGINT        NOT NULL IDENTITY(1, 1),
    SourceId               NVARCHAR(100) NULL,
    -- Vehicle identity (YMME as names) --
    Vin                    NVARCHAR(17)  NULL,
    [Year]                 NVARCHAR(50)  NULL,
    Make                   NVARCHAR(100) NULL,
    Model                  NVARCHAR(100) NULL,
    EngineType             NVARCHAR(100) NULL,
    Trim                   NVARCHAR(100) NULL,
    Transmission           NVARCHAR(50)  NULL,
    UsbProductId           INT           NULL,
    -- Diagnostic snapshot --
    ToolMilStatus          INT           NULL,          -- 0=Unknown, 1=On, 3=Off
    PrimaryDtc             NVARCHAR(50)  NULL,
    ToolLEDStatus          INT           NULL,
    Sil                    NVARCHAR(30)  NULL,
    ABSLightStatus         NVARCHAR(30)  NULL,
    TPMSLightStatus        NVARCHAR(30)  NULL,
    BatteryConditionStatus NVARCHAR(50)  NULL,
    BatteryVoltage         NVARCHAR(20)  NULL,
    Odometer               INT           NULL,          -- NA -> NULL
    Mileage                INT           NULL,
    -- Presence flags (computed at import) --
    HasRecordTime          BIT           NOT NULL CONSTRAINT DF_VehicleProfile_HasRecordTime DEFAULT (0),
    HasFreezeFrames        BIT           NOT NULL CONSTRAINT DF_VehicleProfile_HasFreezeFrames DEFAULT (0),
    HasMonitors            BIT           NOT NULL CONSTRAINT DF_VehicleProfile_HasMonitors DEFAULT (0),
    -- Housekeeping --
    Notes                  NVARCHAR(500) NULL,
    IsActive               BIT           NOT NULL CONSTRAINT DF_VehicleProfile_IsActive DEFAULT (1),
    CreatedDate            DATETIME2     NOT NULL CONSTRAINT DF_VehicleProfile_CreatedDate DEFAULT (getutcdate()),
    UpdatedDate            DATETIME2     NULL,
    CONSTRAINT PK_VehicleProfile PRIMARY KEY (Id)
);
GO

CREATE INDEX IX_VehicleProfile_Vin ON dbo.VehicleProfile (Vin);
GO
CREATE INDEX IX_VehicleProfile_Ymme ON dbo.VehicleProfile (Make, Model, [Year], EngineType);
GO
CREATE INDEX IX_VehicleProfile_SourceId ON dbo.VehicleProfile (SourceId);
GO

/* Payload — full payload(s), gzip-compressed; de-dup by PayloadHash. */
CREATE TABLE dbo.Payload
(
    Id               BIGINT         NOT NULL IDENTITY(1, 1),
    VehicleProfileId BIGINT         NOT NULL,
    Name             NVARCHAR(200)  NULL,
    PayloadData      VARBINARY(MAX) NOT NULL,
    Compression      NVARCHAR(10)   NOT NULL CONSTRAINT DF_Payload_Compression DEFAULT ('gzip'),
    ByteSize         INT            NULL,
    Source           NVARCHAR(50)   NULL,
    PayloadHash      CHAR(64)       NULL,
    CreatedDate      DATETIME2      NOT NULL CONSTRAINT DF_Payload_CreatedDate DEFAULT (getutcdate()),
    CONSTRAINT PK_Payload PRIMARY KEY (Id),
    CONSTRAINT FK_Payload_VehicleProfile FOREIGN KEY (VehicleProfileId) REFERENCES dbo.VehicleProfile (Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_Payload_VehicleProfile ON dbo.Payload (VehicleProfileId);
GO
CREATE UNIQUE INDEX UQ_Payload_Hash ON dbo.Payload (PayloadHash) WHERE PayloadHash IS NOT NULL;
GO

/* RawBuffer — per-segment raw buffers. */
CREATE TABLE dbo.RawBuffer
(
    Id               BIGINT         NOT NULL IDENTITY(1, 1),
    VehicleProfileId BIGINT         NOT NULL,
    BufferType       NVARCHAR(50)   NOT NULL,
    SegmentIndex     INT            NULL,
    RawData          VARBINARY(MAX) NOT NULL,
    Compression      NVARCHAR(10)   NOT NULL CONSTRAINT DF_RawBuffer_Compression DEFAULT ('gzip'),
    ByteSize         INT            NULL,
    CreatedDate      DATETIME2      NOT NULL CONSTRAINT DF_RawBuffer_CreatedDate DEFAULT (getutcdate()),
    CONSTRAINT PK_RawBuffer PRIMARY KEY (Id),
    CONSTRAINT FK_RawBuffer_VehicleProfile FOREIGN KEY (VehicleProfileId) REFERENCES dbo.VehicleProfile (Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_RawBuffer_VehicleProfile ON dbo.RawBuffer (VehicleProfileId);
GO

/* VehicleDtc — DTC / OEM codes (search). */
CREATE TABLE dbo.VehicleDtc
(
    Id               BIGINT         NOT NULL IDENTITY(1, 1),
    VehicleProfileId BIGINT         NOT NULL,
    Source           NVARCHAR(20)   NOT NULL,
    Code             NVARCHAR(50)   NOT NULL,
    StatusType       NVARCHAR(100)  NULL,
    [System]         NVARCHAR(1000) NULL,
    SubSystem        NVARCHAR(1000) NULL,
    Definition       NVARCHAR(1000) NULL,
    CreatedDate      DATETIME2      NOT NULL CONSTRAINT DF_VehicleDtc_CreatedDate DEFAULT (getutcdate()),
    CONSTRAINT PK_VehicleDtc PRIMARY KEY (Id),
    CONSTRAINT FK_VehicleDtc_VehicleProfile FOREIGN KEY (VehicleProfileId) REFERENCES dbo.VehicleProfile (Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_VehicleDtc_VehicleProfile ON dbo.VehicleDtc (VehicleProfileId);
GO
CREATE INDEX IX_VehicleDtc_Code ON dbo.VehicleDtc (Code);
GO

/* Restart the import from newest. */
DELETE FROM dbo.DataLibraryImportState;
GO

COMMIT TRANSACTION;
GO
