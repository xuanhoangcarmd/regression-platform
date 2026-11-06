USE RegressionDB;
GO

/* ----------------------------------------------------------------------------
   Add the Advance Auto Parts project (reference data).
   Idempotent: only inserts when the Code is not already present, so it is safe to rerun.
   ---------------------------------------------------------------------------- */
INSERT INTO dbo.Project (Code, Name, Description)
SELECT s.Code, s.Name, s.Description
FROM (VALUES
    (N'AAP', N'AdvanceAutoParts', N'Advance Auto Parts')
) AS s (Code, Name, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Project p WHERE p.Code = s.Code);
GO
