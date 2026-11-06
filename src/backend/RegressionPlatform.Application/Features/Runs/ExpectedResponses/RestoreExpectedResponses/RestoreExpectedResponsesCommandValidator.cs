using FluentValidation;

namespace RegressionPlatform.Application.Features.Runs.ExpectedResponses.RestoreExpectedResponses;

public sealed class RestoreExpectedResponsesCommandValidator : AbstractValidator<RestoreExpectedResponsesCommand>
{
    public RestoreExpectedResponsesCommandValidator()
    {
        RuleFor(x => x.RequestIds)
            .NotEmpty()
            .Must(ids => ids.Count <= ExpectedResponseLimits.MaxBatch)
            .WithMessage($"At most {ExpectedResponseLimits.MaxBatch} requests per call.");
    }
}
