# Purpose

Describe all database tables. Columns reflect the **actual** `RegressionDB` schema.

---

# Api

API endpoint definitions.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `int` IDENTITY | PK |
| `ProjectId` | `int` | FK → `Project` (`FK_Api_Project`) |
| `Name` | `nvarchar(200)` | NOT NULL; unique **per project** (`UQ_Api_Project_Name`) |
| `Method` | `varchar(10)` | NOT NULL (GET, POST, …) |
| `Endpoint` | `nvarchar(500)` | NOT NULL |
| `Description` | `nvarchar(1000)` | NULL |
| `IsActive` | `bit` | DEFAULT 1 |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |
| `UpdatedDate` | `datetime2` | NULL |

---

# Dataset

A named collection of test requests, scoped to a project and spanning many APIs (e.g.
"Mobile Regression"). Each `ApiRequest` belongs to exactly one dataset. Added by
`database/20260629_AddDataset.sql`.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `int` IDENTITY | PK |
| `ProjectId` | `int` | FK → `Project` (`FK_Dataset_Project`); index `IX_Dataset_Project` |
| `Name` | `nvarchar(200)` | NOT NULL; unique **per project** (`UQ_Dataset_Project_Name`) |
| `Description` | `nvarchar(1000)` | NULL |
| `IsActive` | `bit` | DEFAULT 1 (Inactive datasets are hidden when creating a run) |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |
| `UpdatedDate` | `datetime2` | NULL |

The migration creates one `Default` dataset per project and backfills all existing requests/runs to it.

---

# ApiRequest

Reusable, versioned test requests, grouped into a `Dataset`. A request keeps its `ApiId`
(which API it calls) **and** its `DatasetId` (which dataset it belongs to); both must be in the
same project (enforced in the application layer).

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `bigint` IDENTITY | PK |
| `DatasetId` | `int` | FK → `Dataset` (`FK_ApiRequest_Dataset`); index `IX_ApiRequest_Dataset` |
| `ApiId` | `int` | FK → `Api`; index `IX_ApiRequest_ApiId` |
| `Title` | `nvarchar(300)` | NOT NULL |
| `HeadersJson` | `nvarchar(MAX)` | NULL |
| `QueryStringJson` | `nvarchar(MAX)` | NULL |
| `RequestBody` | `nvarchar(MAX)` | NULL |
| `Version` | `int` | DEFAULT 1 |
| `IsActive` | `bit` | DEFAULT 1 |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |

---

# Run

A regression run (execution).

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `bigint` IDENTITY | PK |
| `ProjectId` | `int` | FK → `Project` (`FK_Run_Project`) |
| `DatasetId` | `int` | FK → `Dataset` (`FK_Run_Dataset`); the dataset this run executes; index `IX_Run_Dataset` |
| `Title` | `nvarchar(300)` | NOT NULL |
| `Description` | `nvarchar(MAX)` | NULL |
| `Status` | `varchar(20)` | NOT NULL |
| `StartTime` | `datetime2` | NULL |
| `EndTime` | `datetime2` | NULL |
| `DurationMs` | `bigint` | NULL |
| `TotalRequest` | `int` | NULL |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |
| `ExecutorInstance` | `nvarchar(200)` | NULL; API process that owns the run (`<node>\|<pid>\|<id>`), set on Start / Re-run and when an executor claims it (`20261119_AddRunExecutorOwnership.sql`) |
| `HeartbeatAt` | `datetime2(3)` | NULL; refreshed by the owning executor every ~20 s while Running. Recovery fails a Running run only when this is older than 2 min, so instances sharing the DB don't fail each other's runs |

---

# RunResponse

One captured response per request execution.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `bigint` IDENTITY | PK |
| `RunId` | `bigint` | FK → `Run` |
| `RequestId` | `bigint` | FK → `ApiRequest` |
| `StatusCode` | `int` | NULL |
| `DurationMs` | `int` | NULL |
| `ResponseHash` | `char(64)` | NULL (SHA-256 of canonical JSON) |
| `ResponseJson` | `nvarchar(MAX)` | NULL (stored response body) |
| `ResponseSize` | `int` | NULL |
| `ErrorMessage` | `nvarchar(MAX)` | NULL |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |

Indexes: `IX_RunResponse_Request (RequestId)`, unique `UX_Run_Request (RunId, RequestId)`.

---

# CompareJob

Async compare job tracking for two runs.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `bigint` IDENTITY | PK |
| `RunAId` | `bigint` | NOT NULL |
| `RunBId` | `bigint` | NOT NULL |
| `Status` | `varchar(20)` | NOT NULL |
| `Progress` | `int` | DEFAULT 0 |
| `CompareResultId` | `bigint` | NULL (set when finished) |
| `StartedDate` | `datetime2` | NULL |
| `FinishedDate` | `datetime2` | NULL |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |

---

# CompareResult

Compare summary for Run A vs Run B.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `bigint` IDENTITY | PK |
| `RunAId` | `bigint` | FK → `Run` |
| `RunBId` | `bigint` | FK → `Run` |
| `TotalRequest` | `int` | NOT NULL |
| `PassCount` | `int` | NOT NULL |
| `ChangedCount` | `int` | NOT NULL |
| `FailedCount` | `int` | NOT NULL |
| `AddedCount` | `int` | NOT NULL |
| `RemovedCount` | `int` | NOT NULL |
| `ModifiedCount` | `int` | NOT NULL |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |

Unique index `UX_Compare_Run (RunAId, RunBId)`.

---

# CompareDetail

Per-request compare outcome and field-level diff.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `bigint` IDENTITY | PK |
| `CompareResultId` | `bigint` | FK → `CompareResult` |
| `RequestId` | `bigint` | FK → `ApiRequest` |
| `Result` | `varchar(20)` | NOT NULL (Pass, Changed, Added, Removed, …) |
| `DiffJson` | `nvarchar(MAX)` | NULL (field-level differences as JSON) |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |

Indexes: `IX_CompareDetail_Request (RequestId)`, `IX_CompareDetail_Result (CompareResultId)`.

---

# CompareIgnoreRule

Configurable JSON paths to ignore during comparison.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `int` IDENTITY | PK |
| `ProjectId` | `int` | FK → `Project` (`FK_CompareIgnoreRule_Project`) |
| `JsonPath` | `nvarchar(500)` | NOT NULL (e.g. `$.timestamp`) |
| `Enabled` | `bit` | DEFAULT 1 |
| `Description` | `nvarchar(500)` | NULL |

A rule applies to the APIs linked via `CompareIgnoreRuleApi`; **no links = applies to all APIs**.
(The former `IgnoreOrder` column was dropped — see `database/20260623_ScopeIgnoreRulesToApis.sql`.)

---

# CompareIgnoreRuleApi

Many-to-many link: which APIs an ignore rule applies to.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `int` IDENTITY | PK |
| `IgnoreRuleId` | `int` | FK → `CompareIgnoreRule` (`FK_CompareIgnoreRuleApi_CompareIgnoreRule`, ON DELETE CASCADE) |
| `ApiId` | `int` | FK → `Api` (`FK_CompareIgnoreRuleApi_Api`, ON DELETE CASCADE) |

Unique `UQ_CompareIgnoreRuleApi_Rule_Api (IgnoreRuleId, ApiId)` (also the FK index on `IgnoreRuleId`).

---

# Report

A point-in-time regression report for a single Completed compare (**1 Report = 1 Compare**).
Headline counts + a computed `RegressionScore`/`RiskLevel` are snapshotted at generation time.
Generated on demand ("Generate Report") and re-generated in place (UPSERT keyed by `CompareJobId`).
Added by `database/20260701_CreateReport.sql`.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `bigint` IDENTITY | PK |
| `ProjectId` | `int` | FK → `Project` (`FK_Report_Project`); index `IX_Report_Project` |
| `CompareJobId` | `bigint` | FK → `CompareJob` (`FK_Report_CompareJob`); unique `UQ_Report_CompareJob` (one report per compare) |
| `CompareResultId` | `bigint` | FK → `CompareResult` (`FK_Report_CompareResult`); the finished result the snapshot was taken from |
| `Name` | `nvarchar(200)` | NOT NULL — e.g. "Regression Report — {RunA} vs {RunB}" |
| `Status` | `varchar(20)` | NOT NULL DEFAULT `'Completed'` (async AI phases may add Pending/Generating) |
| `RegressionScore` | `decimal(5,2)` | NOT NULL — 0–100; `round(UnchangedCount / max(TotalRequest,1) * 100, 1)` |
| `RiskLevel` | `varchar(20)` | NOT NULL — `CK_Report_RiskLevel` IN (`Low`, `Medium`, `High`, `Critical`) |
| `TotalRequest` | `int` | NOT NULL — snapshot of `CompareResult` counts |
| `UnchangedCount` | `int` | NOT NULL |
| `ChangedCount` | `int` | NOT NULL |
| `AddedCount` | `int` | NOT NULL |
| `RemovedCount` | `int` | NOT NULL |
| `FailedCount` | `int` | NOT NULL |
| `TotalApis` | `int` | NOT NULL — distinct APIs in the compare |
| `BreakingApiCount` | `int` | NOT NULL — APIs with ≥1 `FieldRemoved`/`TypeChanged` diff |
| `GeneratedDate` | `datetime2` | NOT NULL DEFAULT `getutcdate()` — bumped on regenerate |
| `CreatedDate` | `datetime2` | NOT NULL DEFAULT `getutcdate()` |

Score/Risk are computed in the application layer (tunable heuristics): Risk from `BreakingApiCount`
(`0 → Low`, `1–2 → Medium`, `3–5 → High`, `>5 → Critical`). AI summary / export / email / share
are planned for later phases.

---

# Project

Minimal project identity. First concrete step of the planned multi-project feature. Scopes
configuration, `Api`, and `Dataset` (and via `Dataset`, every `ApiRequest`). Added by
`database/20260623_Create_ProjectConfiguration.sql`.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `int` IDENTITY | PK |
| `Code` | `nvarchar(50)` | NOT NULL; unique (`UQ_Project_Code`); route slug, e.g. `oreilly` |
| `Name` | `nvarchar(200)` | NOT NULL |
| `Description` | `nvarchar(1000)` | NULL |
| `IsActive` | `bit` | DEFAULT 1 |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |
| `UpdatedDate` | `datetime2` | NULL |

Seeded with `oreilly`, `autozone`, `carid`, `internal`.

---

# ProjectConfiguration

Per-project, per-environment configuration: the Base URL for an environment.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `int` IDENTITY | PK |
| `ProjectId` | `int` | FK → `Project` (`FK_ProjectConfiguration_Project`, ON DELETE CASCADE) |
| `Environment` | `varchar(20)` | NOT NULL; `CK_ProjectConfiguration_Environment` IN (`DEV`, `STG`, `PROD`) |
| `BaseUrl` | `nvarchar(500)` | NULL |
| `IsActive` | `bit` | DEFAULT 1 |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |
| `UpdatedDate` | `datetime2` | NULL |

Unique `UQ_ProjectConfiguration_Project_Environment (ProjectId, Environment)` — one row per
environment per project (also serves as the FK index on `ProjectId`).

---

# ProjectConfigurationHeader

Headers applied to every request in a configuration's environment.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `int` IDENTITY | PK |
| `ConfigurationId` | `int` | FK → `ProjectConfiguration` (`FK_ProjectConfigurationHeader_ProjectConfiguration`, ON DELETE CASCADE) |
| `HeaderKey` | `nvarchar(100)` | NOT NULL |
| `HeaderValue` | `nvarchar(2000)` | NULL — plaintext, **non-secret only** |
| `SecretValueEncrypted` | `varbinary(MAX)` | NULL — AES ciphertext, **secret only** (never plaintext) |
| `IsSecret` | `bit` | DEFAULT 0 |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |
| `UpdatedDate` | `datetime2` | NULL |

- `CK_ProjectConfigurationHeader_SecretStorage` — a secret keeps `HeaderValue` NULL (value lives
  encrypted in `SecretValueEncrypted`); a non-secret keeps `SecretValueEncrypted` NULL. This
  enforces the rule that secrets are never stored in plaintext (see `docs/rules/database/security.md`).
- Unique `UQ_ProjectConfigurationHeader_Configuration_HeaderKey (ConfigurationId, HeaderKey)` —
  header keys are unique within a configuration (also serves as the FK index on `ConfigurationId`).

---

# Data Library (global — not project-scoped)

A shared, top-level catalog of vehicle payloads/raw buffers that users search by YMME / DTC / MIL
to pull a payload for testing. **Not** scoped to a project. Added by
`database/20260629_CreateDataLibrary.sql`.

## VehicleProfile

One vehicle scenario (the searchable parent). YMME is stored as **names** (no enum ids); fields are
populated incrementally.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `bigint` IDENTITY | PK |
| `Vin` | `nvarchar(17)` | NULL; index `IX_VehicleProfile_Vin` |
| `Year` | `nvarchar(50)` | NULL |
| `Make` | `nvarchar(100)` | NULL |
| `Model` | `nvarchar(100)` | NULL |
| `Engine` | `nvarchar(100)` | NULL |
| `Trim` | `nvarchar(100)` | NULL |
| `Transmission` | `nvarchar(50)` | NULL |
| `UsbProductId` | `int` | NULL — capture device USB Product ID |
| `Mil` | `bit` | NULL — MIL on/off (NULL = unknown) |
| `MilDtc` | `nvarchar(50)` | NULL — PrimaryDtc (the MIL-causing DTC) |
| `FreezeFrameDtc` | `nvarchar(20)` | NULL — FF_DTC, e.g. "P0420" |
| `LedStatus` | `nvarchar(30)` | NULL — Red / Yellow / Green |
| `Sil` | `nvarchar(30)` | NULL — ON / OFF / Not Supported |
| `AbsLight` | `nvarchar(30)` | NULL — ON / OFF / Not Supported |
| `TpmsLight` | `nvarchar(30)` | NULL — ON / OFF / Not Supported |
| `BatteryCondition` | `nvarchar(50)` | NULL — e.g. "Battery Good" |
| `BatteryVoltage` | `nvarchar(20)` | NULL — e.g. "11.65 V" |
| `OdometerValue` | `int` | NULL |
| `OdometerUnit` | `nvarchar(10)` | NULL — miles / km |
| `Mileage` | `int` | NULL — ToolInformation.Mileage |
| `RecordTimeJson` | `nvarchar(MAX)` | NULL — RecordTime object as JSON (per-segment ms) |
| `Notes` | `nvarchar(500)` | NULL |
| `IsActive` | `bit` | DEFAULT 1 |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |
| `UpdatedDate` | `datetime2` | NULL |

Index `IX_VehicleProfile_Ymme (Make, Model, Year, Engine)` for YMME search.

## Payload

Full payload(s) for a profile. 1 profile → many.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `bigint` IDENTITY | PK |
| `VehicleProfileId` | `bigint` | FK → `VehicleProfile` (`FK_Payload_VehicleProfile`, ON DELETE CASCADE); index `IX_Payload_VehicleProfile` |
| `Name` | `nvarchar(200)` | NULL |
| `PayloadData` | `varbinary(MAX)` | NOT NULL — **GZip-compressed** payload bytes (compressed in the app; base64 at the API boundary) |
| `Compression` | `nvarchar(10)` | NOT NULL DEFAULT `'gzip'` — gzip / none |
| `ByteSize` | `int` | NULL — original (uncompressed) size in bytes |
| `Source` | `nvarchar(50)` | NULL (e.g. Generated / Imported) |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |

## RawBuffer

Per-segment raw buffers for a profile. 1 profile → many.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `bigint` IDENTITY | PK |
| `VehicleProfileId` | `bigint` | FK → `VehicleProfile` (`FK_RawBuffer_VehicleProfile`, ON DELETE CASCADE); index `IX_RawBuffer_VehicleProfile` |
| `BufferType` | `nvarchar(50)` | NOT NULL (VinProfileRaw, MonitorStatusEcmRaw, FreezeFrameEcmRaw, DtcsEcmRaw, OemModuleRaw, …) |
| `SegmentIndex` | `int` | NULL — ordering within a list (e.g. OemModule blocks) |
| `RawData` | `varbinary(MAX)` | NOT NULL — **GZip-compressed** raw bytes |
| `Compression` | `nvarchar(10)` | NOT NULL DEFAULT `'gzip'` — gzip / none |
| `ByteSize` | `int` | NULL — original (uncompressed) size in bytes |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |

## VehicleDtc

DTC / OEM codes for a profile — powers DTC/OEM-code search. 1 profile → many.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `bigint` IDENTITY | PK |
| `VehicleProfileId` | `bigint` | FK → `VehicleProfile` (`FK_VehicleDtc_VehicleProfile`, ON DELETE CASCADE); index `IX_VehicleDtc_VehicleProfile` |
| `Source` | `nvarchar(20)` | NOT NULL — ECU / TCU / OEM |
| `Code` | `nvarchar(50)` | NOT NULL — e.g. P0420, C1101 (OEM code); index `IX_VehicleDtc_Code` |
| `StatusType` | `nvarchar(100)` | NULL — e.g. Stored, Current/MIL |
| `System` | `nvarchar(1000)` | NULL — decode SystemName (can be a long concatenation) |
| `SubSystem` | `nvarchar(1000)` | NULL — decode SubsystemName |
| `Definition` | `nvarchar(1000)` | NULL |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |

## VehicleMonitor

ECU/TCU readiness monitors for a profile. 1 profile → many.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `bigint` IDENTITY | PK |
| `VehicleProfileId` | `bigint` | FK → `VehicleProfile` (`FK_VehicleMonitor_VehicleProfile`, ON DELETE CASCADE); index `IX_VehicleMonitor_VehicleProfile` |
| `Source` | `nvarchar(10)` | NOT NULL — ECU / TCU |
| `Name` | `nvarchar(150)` | NOT NULL — e.g. "Catalyst Monitoring" |
| `Key` | `int` | NOT NULL — 1=ON(MIL), 2=Not Supported, 3=Not Complete, 4=Complete/Ready |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |

## VehicleFreezeFrame

ECU/TCU freeze-frame items for a profile. 1 profile → many.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `bigint` IDENTITY | PK |
| `VehicleProfileId` | `bigint` | FK → `VehicleProfile` (`FK_VehicleFreezeFrame_VehicleProfile`, ON DELETE CASCADE); index `IX_VehicleFreezeFrame_VehicleProfile` |
| `Source` | `nvarchar(10)` | NOT NULL — ECU / TCU |
| `Name` | `nvarchar(200)` | NOT NULL |
| `Unit` | `nvarchar(50)` | NULL |
| `Pid` | `int` | NULL |
| `ValuesJson` | `nvarchar(MAX)` | NULL — item values (list) as JSON |
| `CreatedDate` | `datetime2` | DEFAULT `getutcdate()` |
