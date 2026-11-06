namespace RegressionPlatform.Application.Features.Compare;

/// <summary>Queues a compare job for asynchronous execution by the background worker.</summary>
public interface ICompareExecutionQueue
{
    ValueTask EnqueueAsync(long compareJobId, CancellationToken cancellationToken);
}
