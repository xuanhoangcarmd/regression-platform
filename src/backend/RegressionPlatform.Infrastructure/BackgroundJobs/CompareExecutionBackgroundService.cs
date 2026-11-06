using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RegressionPlatform.Infrastructure.Services;

namespace RegressionPlatform.Infrastructure.BackgroundJobs;

/// <summary>
/// Drains the <see cref="CompareExecutionQueue"/> and executes each compare job in its own DI scope.
/// One at a time (SingleReader); failures are isolated so the worker keeps running.
/// </summary>
public sealed class CompareExecutionBackgroundService(
    CompareExecutionQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<CompareExecutionBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var executor = scope.ServiceProvider.GetRequiredService<CompareExecutor>();
                await executor.ExecuteAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Executor handles its own failures; this is a last-resort guard so the loop survives.
                logger.LogError(ex, "Unhandled error executing compare job {JobId}.", jobId);
            }
        }
    }
}
