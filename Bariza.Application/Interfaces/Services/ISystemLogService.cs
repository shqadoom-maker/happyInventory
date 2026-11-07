using Bariza.Application.Common;
using Bariza.Application.DTOs.SystemLogs;

namespace Bariza.Application.Interfaces.Services;

public interface ISystemLogService
{
    Task<PagedResult<SystemLogDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? level,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? userId,
        CancellationToken cancellationToken = default);

    Task<SystemLogDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<SystemLogCleanupResult> DeleteOlderThanAsync(
        int olderThanDays,
        CancellationToken cancellationToken = default);
}
