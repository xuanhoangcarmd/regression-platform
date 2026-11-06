using System.Reflection;
using FluentValidation;
using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior: runs all FluentValidation validators for a request
/// before its handler. On failure it short-circuits to a ValidationFailure result
/// (never throws) so the outcome flows through the Result pattern.
/// Applies only to requests whose response is a <see cref="Result"/>.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = results
            .SelectMany(r => r.Errors)
            .Where(failure => failure is not null)
            .Select(failure => new Error(failure.PropertyName, failure.ErrorMessage))
            .ToList();

        if (errors.Count == 0)
        {
            return await next();
        }

        return CreateValidationFailure(errors);
    }

    private static TResponse CreateValidationFailure(IReadOnlyList<Error> errors)
    {
        var responseType = typeof(TResponse);

        // Result<T> declares its own ValidationFailure factory — invoke it for typed responses.
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var method = responseType.GetMethod(
                nameof(Result.ValidationFailure),
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
                binder: null,
                types: [typeof(IReadOnlyList<Error>)],
                modifiers: null
            );
            return (TResponse)method!.Invoke(null, [errors])!;
        }

        return (TResponse)(object)Result.ValidationFailure(errors);
    }
}
