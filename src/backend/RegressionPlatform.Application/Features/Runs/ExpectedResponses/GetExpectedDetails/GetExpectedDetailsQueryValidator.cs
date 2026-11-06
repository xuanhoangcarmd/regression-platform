using FluentValidation;

namespace RegressionPlatform.Application.Features.Runs.ExpectedResponses.GetExpectedDetails;

public sealed class GetExpectedDetailsQueryValidator : AbstractValidator<GetExpectedDetailsQuery>
{
    public GetExpectedDetailsQueryValidator()
    {
        RuleFor(x => x.RequestIds)
            .NotEmpty()
            .Must(ids => ids.Count <= ExpectedResponseLimits.MaxBatch)
            .WithMessage($"At most {ExpectedResponseLimits.MaxBatch} requests per call.");
    }
}
