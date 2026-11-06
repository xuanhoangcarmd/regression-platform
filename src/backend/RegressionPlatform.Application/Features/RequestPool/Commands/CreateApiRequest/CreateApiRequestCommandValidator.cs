using FluentValidation;

namespace RegressionPlatform.Application.Features.RequestPool.Commands.CreateApiRequest;

public sealed class CreateApiRequestCommandValidator : AbstractValidator<CreateApiRequestCommand>
{
    public CreateApiRequestCommandValidator()
    {
        RuleFor(x => x.DatasetId).GreaterThan(0);
        RuleFor(x => x.ApiId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
