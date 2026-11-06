namespace RegressionPlatform.Application.Features.Datasets.Contracts;

/// <summary>A row in the Dataset List: the dataset plus its request and (distinct) API counts.</summary>
public sealed record DatasetListItemResponse(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    int RequestCount,
    int ApiCount
);
