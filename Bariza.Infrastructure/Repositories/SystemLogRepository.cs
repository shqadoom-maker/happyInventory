using Bariza.Application.Interfaces.Repositories;
using Bariza.Core.Entities;
using Bariza.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bariza.Infrastructure.Repositories;

public class SystemLogRepository : ISystemLogRepository
{
    private readonly AppDbContext _db;

    public SystemLogRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<SystemLog> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? level,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.SystemLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(level))
        {
            query = query.Where(log => log.Level == level);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(log => log.TimestampUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(log => log.TimestampUtc <= toUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var normalizedUserId = userId.Trim();
            query = query.Where(log => log.UserId == normalizedUserId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";

            // SQLite LIKE is case-insensitive for ASCII, which keeps searches
            // for paths, logger names and messages convenient.
            query = query.Where(log =>
                EF.Functions.Like(log.Message, pattern) ||
                (log.MessageTemplate != null && EF.Functions.Like(log.MessageTemplate, pattern)) ||
                (log.Exception != null && EF.Functions.Like(log.Exception, pattern)) ||
                (log.SourceContext != null && EF.Functions.Like(log.SourceContext, pattern)) ||
                (log.RequestPath != null && EF.Functions.Like(log.RequestPath, pattern)) ||
                (log.UserName != null && EF.Functions.Like(log.UserName, pattern)) ||
                (log.UserId != null && EF.Functions.Like(log.UserId, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(log => log.TimestampUtc)
            .ThenByDescending(log => log.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<SystemLog?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return _db.SystemLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(log => log.Id == id, cancellationToken);
    }

    public async Task AddRangeAsync(
        IReadOnlyCollection<SystemLog> logs,
        CancellationToken cancellationToken = default)
    {
        if (logs.Count == 0)
        {
            return;
        }

        await _db.SystemLogs.AddRangeAsync(logs, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> DeleteOlderThanAsync(
        DateTime cutoffUtc,
        CancellationToken cancellationToken = default)
    {
        // A set-based delete keeps retention cheap even when the table is large.
        return _db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM SystemLogs WHERE TimestampUtc < {cutoffUtc}",
            cancellationToken);
    }
}
