/* =============================================================================
   Migration: 20260629_AddVehicleProfileMileage
   Purpose  : Store the decode response's ToolInformation.Mileage on the profile
              as VehicleProfile.Mileage.

   Forward-only and idempotent. Run AFTER 20260629_CreateDataLibrary.sql.
   ============================================================================= */

USE RegressionDB;
GO

IF COL_LENGTH('dbo.VehicleProfile', 'Mileage') IS NULL
    ALTER TABLE dbo.VehicleProfile ADD Mileage INT NULL;
GO
