using Bariza.Application.Interfaces.Repositories;
using Bariza.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bariza.Api.Logging;

/// <summary>
/// Drains <see cref="SystemLogQueue"/> in batches and persists the entries via
/// the application repository. Running the writes in a hosted service keeps
/// Serilog's request-thread cost low and serializes SQLite writes with the rest
/// of the application.
/// </summary>
public sealed class SystemLogWriterService : BackgroundService
{
    private const int BatchSize = 100;

    private readonly SystemLogQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;

    public SystemLogWriterService(SystemLogQueue queue, IServiceScopeFactory scopeFactory)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!await _queue.WaitToReadAsync(stoppingToken))
                {
                    break;
                }

                var batch = new List<SystemLogEntry>(BatchSize);

                while (batch.Count < BatchSize && _queue.TryDequeue(out var entry))
                {
                    batch.Add(entry);
                }

                if (batch.Count == 0)
                {
                    continue;
                }

                try
                {
                    await PersistAsync(batch, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    ReportFailure(exception);

                    // Avoid a hot loop when the database is temporarily locked
                    // or unavailable.
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }
        finally
        {
            // Flush whatever is still queued during a graceful shutdown.
            await DrainAsync();
        }
    }

    private async Task PersistAsync(
        IReadOnlyCollection<SystemLogEntry> entries,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();

        var logs = entries.Select(ToEntity).ToList();
        await repository.AddRangeAsync(logs, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task DrainAsync()
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var batch = new List<SystemLogEntry>(BatchSize);

            while (_queue.TryDequeue(out var entry))
            {
                batch.Add(entry);

                if (batch.Count < BatchSize)
                {
                    continue;
                }

                await PersistAsync(batch, timeout.Token);
                batch.Clear();
            }

            if (batch.Count > 0)
            {
                await PersistAsync(batch, timeout.Token);
            }
        }
        catch (Exception exception)
        {
            ReportFailure(exception);
        }
    }

    private static SystemLog ToEntity(SystemLogEntry entry) => new()
    {
        TimestampUtc = entry.TimestampUtc,
        Level = entry.Level,
        Message = entry.Message,
        MessageTemplate = entry.MessageTemplate,
        Exception = entry.Exception,
        SourceContext = entry.SourceContext,
        UserId = entry.UserId,
        UserName = entry.UserName,
        RequestPath = entry.RequestPath,
        RequestMethod = entry.RequestMethod,
        StatusCode = entry.StatusCode,
        TraceId = entry.TraceId,
        Properties = entry.Properties,
    };

    private static void ReportFailure(Exception exception)
    {
        try
        {
            // Do not use Serilog here: a failing log writer must not enqueue
            // more events and create an infinite loop.
            Console.Error.WriteLine(
                $"[{DateTime.UtcNow:O}] System log persistence failed: " +
                $"{exception.GetType().Name}: {exception.Message}");
        }
        catch
        {
            // Console output is best-effort only.
        }
    }
}
