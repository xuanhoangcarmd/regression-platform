using System.Threading.Channels;
using RegressionPlatform.Application.Features.Runs;

namespace RegressionPlatform.Infrastructure.BackgroundJobs;

/// <summary>
/// In-memory queue of run ids awaiting execution (unbounded channel). Producers enqueue via
/// <see cref="IRunExecutionQueue"/>; the background worker consumes <see cref="Reader"/>.
/// </summary>
public sealed class RunExecutionQueue : IRunExecutionQueue
{
    private readonly Channel<long> _channel = Channel.CreateUnbounded<long>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
    );

    public ChannelReader<long> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(long runId, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(runId, cancellationToken);
}
