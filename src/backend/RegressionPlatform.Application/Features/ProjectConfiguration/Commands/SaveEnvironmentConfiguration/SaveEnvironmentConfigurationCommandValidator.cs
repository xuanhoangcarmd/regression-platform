using FluentValidation;
using RegressionPlatform.Application.Features.ProjectConfiguration.Contracts;

namespace RegressionPlatform.Application.Features.ProjectConfiguration.Commands.SaveEnvironmentConfiguration;

public sealed class SaveEnvironmentConfigurationCommandValidator : AbstractValidator<SaveEnvironmentConfigurationCommand>
{
    public SaveEnvironmentConfigurationCommandValidator()
    {
        RuleFor(x => x.ProjectCode).NotEmpty().MaximumLength(50);

        RuleFor(x => x.Environment)
            .NotEmpty()
            .Must(environment => ConfigurationEnvironments.All.Contains(environment.ToUpperInvariant()))
            .WithMessage($"Environment must be one of: {string.Join(", ", ConfigurationEnvironments.All)}.");

        RuleFor(x => x.BaseUrl).MaximumLength(500);

        RuleForEach(x => x.Headers).ChildRules(header =>
        {
            header.RuleFor(h => h.Key).NotEmpty().MaximumLength(100);
            header.RuleFor(h => h.Value).MaximumLength(2000);
        });
    }
}
