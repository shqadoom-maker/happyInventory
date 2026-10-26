using HappyInventory.Application.Common;
using HappyInventory.Application.DTOs.SystemLogs;
using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Application.Interfaces.Services;
using HappyInventory.Core.Entities;

namespace HappyInventory.Application.Services;

public class SystemLogService : ISystemLogService
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 25;
    private const int MaxRetentionDays = 3650;

    private static readonly string[] AllowedLevels =
    {
        "Verbose", "Debug", "Information", "Warning", "Error", "Fatal",
    };

    private readonly ISystemLogRepository _logs;

    public SystemLogService(ISystemLogRepository logs)
    {
        _logs = logs;
    }

    public async Task<PagedResult<SystemLogDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? level,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);

        var normalizedLevel = NormalizeLevel(level);
        var normalizedFrom = NormalizeUtc(fromUtc);
        var normalizedTo = NormalizeUtc(toUtc);

        if (normalizedFrom.HasValue &&
            normalizedTo.HasValue &&
            normalizedFrom.Value > normalizedTo.Value)
        {
            throw new InvalidOperationException("The start date must be before the end date.");
        }

        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var normalizedUserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();

        var (items, totalCount) = await _logs.GetPagedAsync(
            page,
            pageSize,
            normalizedLevel,
            normalizedSearch,
            normalizedFrom,
            normalizedTo,
            normalizedUserId,
            cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<SystemLogDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
        };
    }

    public async Task<SystemLogDto> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var log = await _logs.GetByIdAsync(id, cancellationToken);

        if (log is null)
        {
            throw new KeyNotFoundException("The requested log entry was not found.");
        }

        return ToDto(log);
    }

    public async Task<SystemLogCleanupResult> DeleteOlderThanAsync(
        int olderThanDays,
        CancellationToken cancellationToken = default)
    {
        if (olderThanDays < 1 || olderThanDays > MaxRetentionDays)
        {
            throw new InvalidOperationException(
                $"Retention days must be between 1 and {MaxRetentionDays}.");
        }

        var cutoffUtc = DateTime.UtcNow.AddDays(-olderThanDays);
        var deletedCount = await _logs.DeleteOlderThanAsync(cutoffUtc, cancellationToken);

        return new SystemLogCleanupResult
        {
            DeletedCount = deletedCount,
            OlderThanDays = olderThanDays,
            CutoffUtc = DateTime.SpecifyKind(cutoffUtc, DateTimeKind.Utc),
        };
    }

    private static string? NormalizeLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level) ||
            string.Equals(level.Trim(), "all", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var match = AllowedLevels.FirstOrDefault(
            candidate => string.Equals(candidate, level.Trim(), StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            throw new InvalidOperationException(
                $"Level must be one of: {string.Join(", ", AllowedLevels)}.");
        }

        return match;
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
        };
    }

    private static SystemLogDto ToDto(SystemLog log) => new()
    {
        Id = log.Id,
        TimestampUtc = log.TimestampUtc.Kind == DateTimeKind.Utc
            ? log.TimestampUtc
            : DateTime.SpecifyKind(log.TimestampUtc, DateTimeKind.Utc),
        Level = log.Level,
        Message = log.Message,
        MessageTemplate = log.MessageTemplate,
        Exception = log.Exception,
        SourceContext = log.SourceContext,
        UserId = log.UserId,
        UserName = log.UserName,
        RequestPath = log.RequestPath,
        RequestMethod = log.RequestMethod,
        StatusCode = log.StatusCode,
        TraceId = log.TraceId,
        Properties = log.Properties,
    };
}
