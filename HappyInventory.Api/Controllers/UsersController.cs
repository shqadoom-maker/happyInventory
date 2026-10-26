using System.Security.Claims;
using HappyInventory.Application.DTOs.Users;
using HappyInventory.Application.Interfaces.Services;
using HappyInventory.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyInventory.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = UserRoles.Admin)]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService users, ILogger<UsersController> logger)
    {
        _users = users;
        _logger = logger;
    }

    private int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _users.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _users.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.CreateAsync(request, cancellationToken);

        _logger.LogInformation(
            "Administrator {ActorId} created user {UserId} ({Username}) with role {Role}.",
            CurrentUserId,
            user.Id,
            user.Username,
            user.Role);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Update(
        int id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _users.UpdateAsync(id, request, CurrentUserId, cancellationToken);

        _logger.LogInformation(
            "Administrator {ActorId} updated user {UserId} ({Username}): role {Role}, active {IsActive}.",
            CurrentUserId,
            updated.Id,
            updated.Username,
            updated.Role,
            updated.IsActive);

        return Ok(updated);
    }

    [HttpPost("{id:int}/change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        int id,
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _users.ChangePasswordAsync(id, request, cancellationToken);

        _logger.LogInformation(
            "Administrator {ActorId} changed the password for user {UserId}.",
            CurrentUserId,
            id);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _users.DeleteAsync(id, CurrentUserId, cancellationToken);

        _logger.LogInformation(
            "Administrator {ActorId} deleted user {UserId}.",
            CurrentUserId,
            id);

        return NoContent();
    }
}
