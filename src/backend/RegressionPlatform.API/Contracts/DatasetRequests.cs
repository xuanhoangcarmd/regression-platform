namespace RegressionPlatform.API.Contracts;

/// <summary>Request body for creating a dataset.</summary>
public sealed record CreateDatasetBody(string Name, string? Description);

/// <summary>Request body for updating a dataset (name, description, active state).</summary>
public sealed record UpdateDatasetBody(string Name, string? Description, bool IsActive);
