namespace RegressionPlatform.Application.Features.Runs;

/// <summary>Queues a run for asynchronous execution by the background worker.</summary>
public interface IRunExecutionQueue
{
    ValueTask EnqueueAsync(long runId, CancellationToken cancellationToken);
}
