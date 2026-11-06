using FluentValidation;

namespace RegressionPlatform.Application.Features.Report.Commands.GenerateReport;

public sealed class GenerateReportCommandValidator : AbstractValidator<GenerateReportCommand>
{
    public GenerateReportCommandValidator()
    {
        RuleFor(x => x.ProjectCode).NotEmpty();
        RuleFor(x => x.CompareId).GreaterThan(0);
    }
}
