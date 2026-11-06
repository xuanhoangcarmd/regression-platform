/* =============================================================================
   Migration: 20260629_WidenVehicleDtcColumns
   Purpose  : Decode SystemName/SubsystemName can be long concatenations (e.g.
              "ABS-Anti-Lock Brake System/ASC-Active Stability Control/ASTC-...").
              Widen System/SubSystem/Definition to NVARCHAR(1000) so DTC inserts no
              longer truncate.

   Forward-only and idempotent. Run AFTER 20260629_CreateDataLibrary.sql.
   ============================================================================= */

USE RegressionDB;
GO

ALTER TABLE dbo.VehicleDtc ALTER COLUMN [System]   NVARCHAR(1000) NULL;
GO
ALTER TABLE dbo.VehicleDtc ALTER COLUMN SubSystem  NVARCHAR(1000) NULL;
GO
ALTER TABLE dbo.VehicleDtc ALTER COLUMN Definition NVARCHAR(1000) NULL;
GO
