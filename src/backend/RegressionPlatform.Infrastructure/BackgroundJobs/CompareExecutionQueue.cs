using System.Threading.Channels;
using RegressionPlatform.Application.Features.Compare;

namespace RegressionPlatform.Infrastructure.BackgroundJobs;

/// <summary>
/// In-memory queue of compare-job ids awaiting execution (unbounded channel). Producers enqueue via
/// <see cref="ICompareExecutionQueue"/>; the background worker consumes <see cref="Reader"/>.
/// </summary>
public sealed class CompareExecutionQueue : ICompareExecutionQueue
{
    private readonly Channel<long> _channel = Channel.CreateUnbounded<long>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
    );

    public ChannelReader<long> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(long compareJobId, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(compareJobId, cancellationToken);
}
