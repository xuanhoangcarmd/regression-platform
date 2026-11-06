using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RegressionPlatform.Application.Features.Runs;
using RegressionPlatform.Infrastructure.Services;

namespace RegressionPlatform.Infrastructure.BackgroundJobs;

/// <summary>
/// Drains the <see cref="RunExecutionQueue"/> and executes each run in its own DI scope.
/// One run at a time (SingleReader); failures are isolated so the worker keeps running.
/// On startup it first recovers runs orphaned by an app restart (the queue is in-memory):
/// still-Running runs are finalized as Failed, still-Queued runs are re-enqueued.
/// </summary>
public sealed class RunExecutionBackgroundService(
    RunExecutionQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<RunExecutionBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverOrphanedRunsAsync(stoppingToken);

        await foreach (var runId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var executor = scope.ServiceProvider.GetRequiredService<RunExecutor>();
                await executor.ExecuteAsync(runId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Executor handles its own failures; this is a last-resort guard so the loop survives.
                logger.LogError(ex, "Unhandled error executing run {RunId}.", runId);
            }
        }
    }

    /// <summary>
    /// The queue/progress/cancellation state is in-memory, so a restart orphans DB rows: a run left
    /// 'Running' was interrupted mid-execution (its responses may be partial) → finalize as Failed;
    /// a run left 'Queued' never started → safe to re-enqueue. Failures here are logged, not fatal —
    /// the worker must still come up (recovery runs again on the next restart).
    /// </summary>
    private async Task RecoverOrphanedRunsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRunExecutionRepository>();

            var failed = await repository.FailInterruptedRunsAsync(DateTime.UtcNow, stoppingToken);
            if (failed > 0)
            {
                logger.LogWarning("Recovered {Count} run(s) interrupted by a restart (marked Failed).", failed);
            }

            var queuedIds = await repository.GetQueuedRunIdsAsync(stoppingToken);
            foreach (var runId in queuedIds)
            {
                await queue.EnqueueAsync(runId, stoppingToken);
            }
            if (queuedIds.Count > 0)
            {
                logger.LogInformation("Re-enqueued {Count} queued run(s) after restart.", queuedIds.Count);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down before recovery finished — nothing to do.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Startup run recovery failed; stale runs will be recovered on the next restart.");
        }
    }
}
