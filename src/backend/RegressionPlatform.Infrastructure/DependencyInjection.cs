using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.ApiDefinition;
using RegressionPlatform.Application.Features.Compare;
using RegressionPlatform.Application.Features.DataLibrary;
using RegressionPlatform.Application.Features.Datasets;
using RegressionPlatform.Application.Features.IgnoreRule;
using RegressionPlatform.Application.Features.ProjectConfiguration;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Report;
using RegressionPlatform.Application.Features.Report.Ai;
using RegressionPlatform.Application.Features.RequestPool;
using RegressionPlatform.Application.Features.Runs;
using RegressionPlatform.Infrastructure.Ai;
using RegressionPlatform.Infrastructure.Ai.Providers;
using RegressionPlatform.Infrastructure.BackgroundJobs;
using RegressionPlatform.Infrastructure.Persistence;
using RegressionPlatform.Infrastructure.Persistence.Repositories;
using RegressionPlatform.Infrastructure.Security;
using RegressionPlatform.Infrastructure.Services;

namespace RegressionPlatform.Infrastructure;

/// <summary>
/// Composition root for the Infrastructure layer. Data-access (Dapper repositories),
/// external services, caching, etc. are registered here as features are implemented.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

        // Repositories
        services.AddScoped<IApiRepository, ApiRepository>();
        services.AddScoped<IApiRequestRepository, ApiRequestRepository>();
        services.AddScoped<IDatasetRepository, DatasetRepository>();
        services.AddScoped<IDataLibraryRepository, DataLibraryRepository>();
        services.AddScoped<IProjectConfigurationRepository, ProjectConfigurationRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IIgnoreRuleRepository, IgnoreRuleRepository>();
        services.AddScoped<IRunRepository, RunRepository>();
        services.AddScoped<IExpectedResponseRepository, ExpectedResponseRepository>();
        services.AddScoped<IRunExecutionRepository, RunExecutionRepository>();
        services.AddScoped<ICompareRepository, CompareRepository>();
        services.AddScoped<ICompareExecutionRepository, CompareExecutionRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();

        // External services
        services.AddHttpClient<IApiSpecImporter, SwaggerApiSpecImporter>();
        services.AddScoped<IExcelRequestParser, ExcelRequestParser>();
        services.AddSingleton<ISecretProtector, AesSecretProtector>();

        // Run execution (background worker): queue + per-request HTTP client + executor.
        services.AddSingleton<RunExecutionQueue>();
        services.AddSingleton<IRunExecutionQueue>(sp => sp.GetRequiredService<RunExecutionQueue>());
        services.AddSingleton<IRunProgressTracker, InMemoryRunProgressTracker>();
        services.AddSingleton<IRunCancellationRegistry, RunCancellationRegistry>();
        services.AddSingleton<IRunExecutorIdentity, RunExecutorIdentity>();
        services.AddHttpClient(RunExecutor.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddScoped<RunExecutor>();
        services.AddScoped<IRunRequestExecutor>(sp => sp.GetRequiredService<RunExecutor>());
        services.AddHostedService<RunExecutionBackgroundService>();

        // Compare execution (background worker): queue + engine.
        services.AddSingleton<CompareExecutionQueue>();
        services.AddSingleton<ICompareExecutionQueue>(sp => sp.GetRequiredService<CompareExecutionQueue>());
        services.AddSingleton<ICompareCancellationRegistry, CompareCancellationRegistry>();
        services.AddScoped<CompareExecutor>();
        services.AddHostedService<CompareExecutionBackgroundService>();

        // AI providers (report AI analysis). Provider is chosen by the AI:Provider config; only
        // DeepSeek + RuleBased are implemented today. Bind options manually (no extra packages).
        var aiSection = configuration.GetSection(AiOptions.SectionName);
        var deepSeekSection = aiSection.GetSection("DeepSeek");
        var aiOptions = new AiOptions
        {
            Provider = aiSection["Provider"] ?? Application.Features.Report.Ai.Contracts.AiProviderNames.RuleBased,
            DeepSeek = new DeepSeekOptions
            {
                ApiKey = deepSeekSection["ApiKey"] ?? string.Empty,
                BaseUrl = deepSeekSection["BaseUrl"] ?? "https://api.deepseek.com",
                Model = deepSeekSection["Model"] ?? "deepseek-chat",
            },
        };
        services.AddSingleton(aiOptions);
        services.AddSingleton<IReportAiCache, ReportAiCache>();
        services.AddSingleton<IReportAiAskCache, ReportAiAskCache>();
        services.AddScoped<IReportAiRepository, ReportAiRepository>();

        services.AddScoped<IAiProvider, RuleBasedProvider>();
        services.AddScoped<IAiProvider, GeminiProvider>();
        services.AddScoped<IAiProvider, ClaudeProvider>();
        services.AddScoped<IAiProvider, OpenAiProvider>();
        services.AddHttpClient<DeepSeekProvider>(client =>
        {
            client.BaseAddress = new Uri(aiOptions.DeepSeek.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddScoped<IAiProvider>(sp => sp.GetRequiredService<DeepSeekProvider>());
        services.AddScoped<IAiProviderResolver, AiProviderResolver>();

        return services;
    }
}
