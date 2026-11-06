namespace RegressionPlatform.Application.Features.RequestPool.Commands.ImportApiRequests;

/// <summary>Summary of an Excel import: rows found in the file, requests created, and rows skipped.</summary>
public sealed record ImportApiRequestsResponse(int Total, int Imported, int Skipped);
