USE RegressionDB;
GO

-- Optional free-text description for a pooled request (shown next to the Title on the Requests page,
-- editable in the request form, filled from an optional "Description" column on Excel import).
IF COL_LENGTH('dbo.ApiRequest', 'Description') IS NULL
    ALTER TABLE dbo.ApiRequest
        ADD Description NVARCHAR(1000) NULL;
GO
