using System.Security.Claims;
using HappyInventory.Application.DTOs.Auth;
using HappyInventory.Application.DTOs.Users;
using HappyInventory.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyInventory.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserService users,
        ITokenService tokens,
        ILogger<AuthController> logger)
    {
        _users = users;
        _tokens = tokens;
        _logger = logger;
    }

    /// <summary>Exchanges credentials for a JWT. The only anonymous endpoint.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        UserDto? user;

        try
        {
            user = await _users.AuthenticateAsync(
                request.Username,
                request.Password,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                "Login rejected for {Username}: account disabled.",
                request.Username);

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication failed",
                Detail = ex.Message,
            };
            problem.Extensions["code"] = "account_disabled";

            return Unauthorized(problem);
        }

        if (user is null)
        {
            _logger.LogWarning(
                "Login failed for {Username}: invalid credentials.",
                request.Username);

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication failed",
                Detail = "Invalid username or password.",
            };
            problem.Extensions["code"] = "invalid_credentials";

            return Unauthorized(problem);
        }

        var (token, expiresUtc) = _tokens.CreateToken(user);

        _logger.LogInformation(
            "User {UserId} ({Username}) signed in with role {Role}.",
            user.Id,
            user.Username,
            user.Role);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresUtc = expiresUtc,
            User = user,
        });
    }

    /// <summary>Returns the signed-in user, refreshing their profile from the database.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(idValue, out var userId))
        {
            return Unauthorized();
        }

        return Ok(await _users.GetByIdAsync(userId, cancellationToken));
    }

    /// <summary>
    /// Changes the current user's password. Returns a fresh token so the client
    /// can continue with an updated session.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(idValue, out var userId))
        {
            return Unauthorized();
        }

        await _users.ChangePasswordAsync(userId, request, cancellationToken);

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        var (token, expiresUtc) = _tokens.CreateToken(user);

        _logger.LogInformation(
            "User {UserId} changed their password.",
            userId);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresUtc = expiresUtc,
            User = user,
        });
    }
}
