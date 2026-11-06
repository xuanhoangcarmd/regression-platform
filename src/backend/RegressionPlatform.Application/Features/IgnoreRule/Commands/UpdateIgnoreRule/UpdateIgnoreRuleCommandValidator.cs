using FluentValidation;
using RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Application.Features.IgnoreRule.Commands.UpdateIgnoreRule;

public sealed class UpdateIgnoreRuleCommandValidator : AbstractValidator<UpdateIgnoreRuleCommand>
{
    public UpdateIgnoreRuleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.JsonPath).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.RuleType)
            .Must(CompareIgnoreRuleTypes.IsValid)
            .WithMessage("RuleType must be 'Ignore', 'ExtractRoot' or 'TrimAfter'.");
        RuleFor(x => x.Delimiter)
            .NotEmpty()
            .MaximumLength(50)
            .When(x => x.RuleType == CompareIgnoreRuleTypes.TrimAfter)
            .WithMessage("Enter the character(s) to cut the value at (e.g. '?').");
    }
}
