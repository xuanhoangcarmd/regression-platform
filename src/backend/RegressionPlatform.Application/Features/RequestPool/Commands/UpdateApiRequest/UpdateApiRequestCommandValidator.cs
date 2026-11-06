using FluentValidation;

namespace RegressionPlatform.Application.Features.RequestPool.Commands.UpdateApiRequest;

public sealed class UpdateApiRequestCommandValidator : AbstractValidator<UpdateApiRequestCommand>
{
    public UpdateApiRequestCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ApiId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
