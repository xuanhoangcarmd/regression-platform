using FluentValidation;
using RegressionPlatform.Application.Features.ApiDefinition.Contracts;

namespace RegressionPlatform.Application.Features.ApiDefinition.Commands.UpdateApi;

public sealed class UpdateApiCommandValidator : AbstractValidator<UpdateApiCommand>
{
    public UpdateApiCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Method)
            .NotEmpty()
            .Must(method => ApiHttpMethods.Allowed.Contains(method))
            .WithMessage($"Method must be one of: {string.Join(", ", ApiHttpMethods.Allowed)}.");
        RuleFor(x => x.Endpoint).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
