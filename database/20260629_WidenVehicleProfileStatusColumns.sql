/* =============================================================================
   Migration: 20260629_WidenVehicleProfileStatusColumns
   Purpose  : The decode API returns status values longer than expected (e.g.
              "Not Supported" / "Not supported" = 13 chars), which overflowed the
              NVARCHAR(10) light/SIL columns. Widen them to NVARCHAR(30).

   Forward-only and idempotent. Run AFTER 20260629_CreateDataLibrary.sql.
   ============================================================================= */

USE RegressionDB;
GO

-- ALTER COLUMN only widens (no data loss); safe to re-run.
ALTER TABLE dbo.VehicleProfile ALTER COLUMN Sil       NVARCHAR(30) NULL;
GO
ALTER TABLE dbo.VehicleProfile ALTER COLUMN AbsLight  NVARCHAR(30) NULL;
GO
ALTER TABLE dbo.VehicleProfile ALTER COLUMN TpmsLight NVARCHAR(30) NULL;
GO
ALTER TABLE dbo.VehicleProfile ALTER COLUMN LedStatus NVARCHAR(30) NULL;
GO
