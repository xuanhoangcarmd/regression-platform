namespace RegressionPlatform.Application.Features.Datasets.Contracts;

/// <summary>An API that has requests in a dataset (for the run-create API picker).</summary>
public sealed record DatasetApiResponse(int Id, string Name, string Method, string Endpoint, int RequestCount);
