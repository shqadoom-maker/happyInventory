using Bariza.Application.Common;
using Bariza.Application.DTOs.SystemLogs;
using Bariza.Application.Interfaces.Services;
using Bariza.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bariza.Api.Controllers;

/// <summary>
/// Read-only access to the structured application logs stored in SQLite.
/// The log viewer is restricted to administrators because entries can contain
/// operational details such as request paths, user names and stack traces.
/// </summary>
[ApiController]
[Route("api/system-logs")]
[Authorize(Roles = UserRoles.Admin)]
public class SystemLogsController : ControllerBase
{
    private readonly ISystemLogService _logs;

    public SystemLogsController(ISystemLogService logs)
    {
        _logs = logs;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SystemLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<SystemLogDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? level = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _logs.GetPagedAsync(
            page,
            pageSize,
            level,
            search,
            fromUtc,
            toUtc,
            userId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(SystemLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SystemLogDto>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        return Ok(await _logs.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>
    /// Removes log entries older than the requested number of days. The
    /// endpoint intentionally has no "delete all" mode.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(typeof(SystemLogCleanupResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SystemLogCleanupResult>> Cleanup(
        [FromQuery] int olderThanDays = 30,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _logs.DeleteOlderThanAsync(olderThanDays, cancellationToken));
    }
}
