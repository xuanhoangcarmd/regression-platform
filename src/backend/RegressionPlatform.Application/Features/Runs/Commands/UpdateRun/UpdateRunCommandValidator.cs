using FluentValidation;

namespace RegressionPlatform.Application.Features.Runs.Commands.UpdateRun;

public sealed class UpdateRunCommandValidator : AbstractValidator<UpdateRunCommand>
{
    public UpdateRunCommandValidator()
    {
        RuleFor(x => x.RunId).GreaterThan(0);
        RuleFor(x => x.DatasetId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Environment).NotEmpty().MaximumLength(20);
        RuleFor(x => x.RunType).NotEmpty().MaximumLength(20);
        RuleFor(x => x.RequestLimitPerApi).GreaterThan(0).When(x => x.RequestLimitPerApi.HasValue);

        // Same parallelism guard as create: each ≥ 1 and the product (effective concurrency) ≤ 100.
        RuleFor(x => x.MaxParallelApis).InclusiveBetween(1, 100);
        RuleFor(x => x.MaxParallelRequestsPerApi).InclusiveBetween(1, 100);
        RuleFor(x => x)
            .Must(x => x.MaxParallelApis * x.MaxParallelRequestsPerApi <= 100)
            .WithMessage("Max parallel APIs × max parallel requests per API must not exceed 100.")
            .WithName("MaxParallel");
    }
}
