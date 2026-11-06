using FluentValidation;
using RegressionPlatform.Application.Features.ApiDefinition.Contracts;

namespace RegressionPlatform.Application.Features.ApiDefinition.Commands.CreateApi;

public sealed class CreateApiCommandValidator : AbstractValidator<CreateApiCommand>
{
    public CreateApiCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Method)
            .NotEmpty()
            .Must(method => ApiHttpMethods.Allowed.Contains(method))
            .WithMessage($"Method must be one of: {string.Join(", ", ApiHttpMethods.Allowed)}.");
        RuleFor(x => x.Endpoint).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
