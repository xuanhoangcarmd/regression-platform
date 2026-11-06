using System.Text.Json;
using FluentValidation;

namespace RegressionPlatform.Application.Features.Runs.ExpectedResponses.SaveExpectedResponses;

public sealed class SaveExpectedResponsesCommandValidator : AbstractValidator<SaveExpectedResponsesCommand>
{
    public SaveExpectedResponsesCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .Must(items => items.Count <= ExpectedResponseLimits.MaxBatch)
            .WithMessage($"At most {ExpectedResponseLimits.MaxBatch} responses per save.");
        RuleFor(x => x.Items)
            .Must(items => items.Select(item => item.RequestId).Distinct().Count() == items.Count)
            .WithMessage("Each request may appear only once.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.RequestId).GreaterThan(0);
            item.RuleFor(i => i.StatusCode).InclusiveBetween(100, 599).When(i => i.StatusCode.HasValue);
            item.RuleFor(i => i.Reason).MaximumLength(ExpectedResponseLimits.ReasonMaxLength);
            item.RuleFor(i => i.ResponseJson)
                .NotEmpty()
                .Must(BeValidJson)
                .WithMessage(i => $"Request {i.RequestId}: the response is not valid JSON.");
        });
    }

    private static bool BeValidJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
