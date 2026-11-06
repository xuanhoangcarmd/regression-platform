using FluentValidation;

namespace RegressionPlatform.Application.Features.Datasets.Commands.UpdateDataset;

public sealed class UpdateDatasetCommandValidator : AbstractValidator<UpdateDatasetCommand>
{
    public UpdateDatasetCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
