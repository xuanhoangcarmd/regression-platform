namespace RegressionPlatform.Application.Features.ApiDefinition.Commands.ImportApis;

/// <summary>Summary of a Swagger import: how many operations were found, imported, and skipped.</summary>
public sealed record ImportApisResponse(int Total, int Imported, int Skipped);
