/* =============================================================================
   Migration: 20260629_AddVehicleProfileMilDtc
   Purpose  : Store the decode response's PrimaryDtc (the MIL-causing DTC) on the
              profile as VehicleProfile.MilDtc.

   Forward-only and idempotent. Run AFTER 20260629_CreateDataLibrary.sql.
   ============================================================================= */

USE RegressionDB;
GO

IF COL_LENGTH('dbo.VehicleProfile', 'MilDtc') IS NULL
    ALTER TABLE dbo.VehicleProfile ADD MilDtc NVARCHAR(50) NULL;  -- PrimaryDtc, e.g. "P0420"
GO
