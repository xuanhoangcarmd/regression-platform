namespace RegressionPlatform.Application.Features.Datasets.Contracts;

/// <summary>A single dataset (for the detail header and edit prefill).</summary>
public sealed record DatasetResponse(int Id, string Name, string? Description, bool IsActive);
