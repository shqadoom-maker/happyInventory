using System.Threading.Channels;

namespace HappyInventory.Api.Logging;

/// <summary>
/// A bounded in-memory queue between Serilog and the database writer. The
/// bounded capacity protects the process from unbounded memory growth if the
/// database is slow or temporarily unavailable.
/// </summary>
public sealed class SystemLogQueue
{
    private readonly Channel<SystemLogEntry> _channel;

    public SystemLogQueue(int capacity = 10_000)
    {
        var boundedCapacity = Math.Clamp(capacity, 100, 100_000);

        _channel = Channel.CreateBounded<SystemLogEntry>(
            new BoundedChannelOptions(boundedCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
            });
    }

    public bool TryEnqueue(SystemLogEntry entry)
    {
        return _channel.Writer.TryWrite(entry);
    }

    public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.WaitToReadAsync(cancellationToken);
    }

    public bool TryDequeue(out SystemLogEntry entry)
    {
        return _channel.Reader.TryRead(out entry!);
    }
}
