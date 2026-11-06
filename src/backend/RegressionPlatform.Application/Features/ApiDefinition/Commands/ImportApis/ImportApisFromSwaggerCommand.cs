using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.ApiDefinition.Commands.ImportApis;

/// <summary>Imports API definitions from an OpenAPI/Swagger document URL into a project.</summary>
public sealed record ImportApisFromSwaggerCommand(string ProjectCode, string SwaggerUrl)
    : IRequest<Result<ImportApisResponse>>;
