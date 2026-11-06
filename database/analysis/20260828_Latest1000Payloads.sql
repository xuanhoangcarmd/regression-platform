USE RegressionDB;
GO

/* ----------------------------------------------------------------------------
   1000 most recently added payloads (newest first), with their vehicle profile.
   Ordered by CreatedDate DESC, Id DESC as a stable tiebreaker.
   ---------------------------------------------------------------------------- */
SELECT TOP (1000)
    p.Id             AS PayloadId,
    p.VehicleProfileId,
    vp.Vin,
    vp.UsbProductId,
    vp.[Year],
    vp.Make,
    vp.Model,
    vp.EngineType,
    p.Name           AS PayloadName,
    p.ByteSize,
    p.Source,
    p.Compression,
    p.CreatedDate
FROM Payload p
INNER JOIN VehicleProfile vp ON vp.Id = p.VehicleProfileId
ORDER BY p.CreatedDate DESC, p.Id DESC;
GO
