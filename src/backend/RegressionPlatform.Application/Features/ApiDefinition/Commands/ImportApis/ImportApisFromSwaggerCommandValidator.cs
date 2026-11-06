using FluentValidation;

namespace RegressionPlatform.Application.Features.ApiDefinition.Commands.ImportApis;

public sealed class ImportApisFromSwaggerCommandValidator : AbstractValidator<ImportApisFromSwaggerCommand>
{
    public ImportApisFromSwaggerCommandValidator()
    {
        RuleFor(x => x.SwaggerUrl)
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Swagger URL must be a valid http/https URL.");
    }
}
