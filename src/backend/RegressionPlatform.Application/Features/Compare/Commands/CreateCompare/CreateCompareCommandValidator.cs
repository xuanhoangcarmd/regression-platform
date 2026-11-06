using FluentValidation;
using RegressionPlatform.Application.Features.Compare.Contracts;

namespace RegressionPlatform.Application.Features.Compare.Commands.CreateCompare;

public sealed class CreateCompareCommandValidator : AbstractValidator<CreateCompareCommand>
{
    public CreateCompareCommandValidator()
    {
        RuleFor(x => x.RunAId).GreaterThan(0);
        RuleFor(x => x.RunBId).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => x.RunAId != x.RunBId)
            .WithName("RunBId")
            .WithMessage("Run A and Run B must be different.");
        RuleFor(x => x.BaselineMode)
            .Must(mode => mode is null || CompareBaselineModes.IsValid(mode))
            .WithMessage($"BaselineMode must be '{CompareBaselineModes.Original}' or '{CompareBaselineModes.Expected}'.");
    }
}
