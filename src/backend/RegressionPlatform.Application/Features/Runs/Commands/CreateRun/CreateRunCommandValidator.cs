using FluentValidation;

namespace RegressionPlatform.Application.Features.Runs.Commands.CreateRun;

public sealed class CreateRunCommandValidator : AbstractValidator<CreateRunCommand>
{
    public CreateRunCommandValidator()
    {
        RuleFor(x => x.DatasetId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Environment).NotEmpty().MaximumLength(20);
        RuleFor(x => x.RunType).NotEmpty().MaximumLength(20);
        RuleFor(x => x.RequestLimitPerApi).GreaterThan(0).When(x => x.RequestLimitPerApi.HasValue);

        // Parallelism knobs. Each ≥ 1; the effective concurrency (product) is capped so a run
        // can't overwhelm the target server. 1 × 1 = sequential (the default).
        RuleFor(x => x.MaxParallelApis).InclusiveBetween(1, 100);
        RuleFor(x => x.MaxParallelRequestsPerApi).InclusiveBetween(1, 100);
        RuleFor(x => x)
            .Must(x => x.MaxParallelApis * x.MaxParallelRequestsPerApi <= 100)
            .WithMessage("Max parallel APIs × max parallel requests per API must not exceed 100.")
            .WithName("MaxParallel");
    }
}
