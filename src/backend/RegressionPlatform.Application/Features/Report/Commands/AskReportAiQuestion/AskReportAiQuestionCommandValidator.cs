using FluentValidation;

namespace RegressionPlatform.Application.Features.Report.Commands.AskReportAiQuestion;

public sealed class AskReportAiQuestionCommandValidator : AbstractValidator<AskReportAiQuestionCommand>
{
    public AskReportAiQuestionCommandValidator()
    {
        RuleFor(command => command.ProjectCode).NotEmpty();
        RuleFor(command => command.Question)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Question must be 500 characters or fewer.");
    }
}
