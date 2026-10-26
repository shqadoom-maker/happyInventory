using HappyInventory.Core.Entities;

namespace HappyInventory.Application.Interfaces.Repositories;

public interface ISystemLogRepository
{
    Task<(IReadOnlyList<SystemLog> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? level,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? userId,
        CancellationToken cancellationToken = default);

    Task<SystemLog?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyCollection<SystemLog> logs,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Deletes rows older than the cutoff and returns the number removed.</summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
