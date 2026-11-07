using Bariza.Api.Logging;
using Xunit;

namespace Bariza.Tests;

public class SystemLogQueueTests
{
    [Fact]
    public void TryEnqueue_WhenFull_DropsOldestAndTracksMetric()
    {
        var queue = new SystemLogQueue(100);

        for (var index = 0; index < 105; index++)
        {
            Assert.True(queue.TryEnqueue(new SystemLogEntry
            {
                Message = index.ToString(),
            }));
        }

        Assert.Equal(100, queue.Capacity);
        Assert.Equal(100, queue.QueuedCount);
        Assert.Equal(5, queue.DroppedCount);

        Assert.True(queue.TryDequeue(out var oldestRemaining));
        Assert.Equal("5", oldestRemaining.Message);
    }
}
