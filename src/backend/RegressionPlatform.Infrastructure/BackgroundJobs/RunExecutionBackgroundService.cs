using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RegressionPlatform.Application.Features.Runs;
using RegressionPlatform.Infrastructure.Services;

namespace RegressionPlatform.Infrastructure.BackgroundJobs;

/// <summary>
/// Drains the <see cref="RunExecutionQueue"/> and executes each run in its own DI scope.
/// One run at a time (SingleReader); failures are isolated so the worker keeps running.
/// Several instances may share the database, so recovery is ownership-aware: on startup this
/// node's still-Queued runs are re-enqueued, and — at startup and then every minute — Running runs
/// whose owner stopped heart-beating (killed / restarted) are finalized as Failed. Runs a live
/// instance is executing (here or on another machine) are never touched.
/// </summary>
public sealed class RunExecutionBackgroundService(
    RunExecutionQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<RunExecutionBackgroundService> logger
) : BackgroundService
{
    /// <summary>How often stale Running runs are swept (after the startup recovery).</summary>
    private static readonly TimeSpan StaleSweepInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverOrphanedRunsAsync(stoppingToken);
        var sweep = SweepStaleRunsAsync(stoppingToken);

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

        await sweep;
    }

    /// <summary>Periodically fails Running runs whose owner stopped heart-beating (any instance).</summary>
    private async Task SweepStaleRunsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var timer = new PeriodicTimer(StaleSweepInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FailStaleRunsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private async Task FailStaleRunsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRunExecutionRepository>();
            var failed = await repository.FailStaleRunsAsync(DateTime.UtcNow, RunExecutor.StaleAfter, stoppingToken);
            if (failed > 0)
            {
                logger.LogWarning(
                    "Marked {Count} run(s) Failed: no heartbeat from their executor for {Minutes} min (process stopped or restarted).",
                    failed,
                    RunExecutor.StaleAfter.TotalMinutes
                );
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stale run sweep failed; retrying on the next tick.");
        }
    }

    /// <summary>
    /// The queue/progress/cancellation state is in-memory, so a restart orphans DB rows: a run left
    /// 'Running' with a stale heartbeat was interrupted mid-execution → finalize as Failed (a fresh
    /// heartbeat means another live instance owns it — leave it); a run this node left 'Queued' never
    /// started → safe to re-enqueue. Failures here are logged, not fatal — the worker must still come
    /// up (the periodic sweep retries the stale part).
    /// </summary>
    private async Task RecoverOrphanedRunsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRunExecutionRepository>();

            var failed = await repository.FailStaleRunsAsync(DateTime.UtcNow, RunExecutor.StaleAfter, stoppingToken);
            if (failed > 0)
            {
                logger.LogWarning("Recovered {Count} run(s) whose executor stopped (marked Failed).", failed);
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
