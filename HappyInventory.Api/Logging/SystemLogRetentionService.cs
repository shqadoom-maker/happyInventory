using HappyInventory.Application.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HappyInventory.Api.Logging;

/// <summary>
/// Periodically prunes old log rows so the SQLite database does not grow
/// without bound. Retention is configurable through
/// <c>Logging:Database:RetentionDays</c>; zero disables automatic pruning.
/// </summary>
public sealed class SystemLogRetentionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SystemLogRetentionService> _logger;
    private readonly int _retentionDays;

    public SystemLogRetentionService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<SystemLogRetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _retentionDays = configuration.GetValue("Logging:Database:RetentionDays", 30);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_retentionDays <= 0)
        {
            return;
        }

        try
        {
            // Let migrations and the first request settle before pruning.
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                var cutoffUtc = DateTime.UtcNow.AddDays(-_retentionDays);

                var deleted = await repository.DeleteOlderThanAsync(cutoffUtc, stoppingToken);

                if (deleted > 0)
                {
                    _logger.LogInformation(
                        "Retention removed {Count} system log entries older than {RetentionDays} days.",
                        deleted,
                        _retentionDays);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                try
                {
                    Console.Error.WriteLine(
                        $"[{DateTime.UtcNow:O}] System log retention failed: " +
                        $"{exception.GetType().Name}: {exception.Message}");
                }
                catch
                {
                    // Console output is best-effort only.
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
