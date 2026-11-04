using System.Threading.Channels;

namespace HappyInventory.Api.Logging;

/// <summary>
/// A bounded in-memory queue between Serilog and the database writer. The
/// bounded capacity protects the process from unbounded memory growth if the
/// database is slow or temporarily unavailable.
/// </summary>
public sealed class SystemLogQueue
{
    private static readonly TimeSpan DropWarningInterval = TimeSpan.FromSeconds(30);

    private readonly Channel<SystemLogEntry> _channel;
    private readonly int _capacity;
    private long _droppedCount;
    private long _lastDropWarningTicks;

    public SystemLogQueue(int capacity = 10_000)
    {
        _capacity = Math.Clamp(capacity, 100, 100_000);

        _channel = Channel.CreateBounded<SystemLogEntry>(
            new BoundedChannelOptions(_capacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
            });
    }

    /// <summary>Number of entries currently waiting to be persisted.</summary>
    public int QueuedCount => _channel.Reader.CanCount ? _channel.Reader.Count : 0;

    /// <summary>Total number of entries dropped because the queue was full.</summary>
    public long DroppedCount => Interlocked.Read(ref _droppedCount);

    public int Capacity => _capacity;

    public bool TryEnqueue(SystemLogEntry entry)
    {
        // BoundedChannelFullMode.DropOldest does not report that it dropped an
        // entry, so observe the queue length before writing. The count is a
        // best-effort metric under concurrent writers.
        if (_channel.Reader.CanCount && _channel.Reader.Count >= _capacity)
        {
            var dropped = Interlocked.Increment(ref _droppedCount);
            ReportQueueFull(dropped);
        }

        return _channel.Writer.TryWrite(entry);
    }

    private void ReportQueueFull(long dropped)
    {
        var now = DateTime.UtcNow.Ticks;
        var lastWarning = Interlocked.Read(ref _lastDropWarningTicks);

        if (now - lastWarning < DropWarningInterval.Ticks)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _lastDropWarningTicks, now, lastWarning) != lastWarning)
        {
            return;
        }

        try
        {
            // Do not use Serilog here: a full queue would enqueue another event
            // and could recursively trigger this warning.
            Console.Error.WriteLine(
                $"[{DateTime.UtcNow:O}] System log queue is full " +
                $"(capacity {_capacity}); oldest entries are being dropped. " +
                $"Total dropped: {dropped}.");
        }
        catch
        {
            // Console output is best-effort only.
        }
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
