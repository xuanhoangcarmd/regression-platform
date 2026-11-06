using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RegressionPlatform.Application.Behaviors;
using RegressionPlatform.Application.Features.Report.Ai;

namespace RegressionPlatform.Application;

/// <summary>
/// Composition root for the Application layer: registers MediatR (CQRS) and
/// FluentValidation. Feature handlers/validators are discovered by assembly scan
/// as they are added.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly);

        // Report AI: shared context builder + provider runner (used by the read, regenerate and Ask-AI handlers).
        services.AddScoped<ReportAiContextBuilder>();
        services.AddScoped<ReportAiRunner>();

        return services;
    }
}
