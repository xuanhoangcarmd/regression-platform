using FluentValidation;

namespace RegressionPlatform.Application.Features.Datasets.Commands.CreateDataset;

public sealed class CreateDatasetCommandValidator : AbstractValidator<CreateDatasetCommand>
{
    public CreateDatasetCommandValidator()
    {
        RuleFor(x => x.ProjectCode).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
