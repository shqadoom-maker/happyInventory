using System.Security.Claims;
using Bariza.Application.Common;
using Bariza.Application.DTOs.Auth;
using Bariza.Application.DTOs.Users;
using Bariza.Application.Interfaces.Services;
using Bariza.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bariza.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "happy_inventory_refresh";
    private const string RefreshTokenCookiePath = "/api/auth";

    private readonly IUserService _users;
    private readonly IAuthSessionService _sessions;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserService users,
        IAuthSessionService sessions,
        IWebHostEnvironment environment,
        ILogger<AuthController> logger)
    {
        _users = users;
        _sessions = sessions;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>Exchanges credentials for a JWT and an HttpOnly refresh cookie.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.AuthenticateAsync(
            request.Username,
            request.Password,
            cancellationToken);

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

        // A successful login replaces whatever session this browser had. Revoke
        // the previous cookie so it cannot be replayed after the new cookie is
        // written.
        var previousRefreshToken = GetRefreshTokenCookie();
        if (!string.IsNullOrWhiteSpace(previousRefreshToken))
        {
            await _sessions.RevokeAsync(previousRefreshToken, cancellationToken);
        }

        var session = await _sessions.CreateAsync(
            user,
            request.RememberMe,
            cancellationToken);

        SetRefreshCookie(session);

        _logger.LogInformation(
            "User {UserId} ({Username}) signed in with role {Role}.",
            user.Id,
            user.Username,
            user.Role);

        return Ok(session.Response);
    }

    /// <summary>
    /// Rotates the refresh token from the HttpOnly cookie and returns a fresh
    /// access token. The old refresh token is revoked by the rotation.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = GetRefreshTokenCookie();

        try
        {
            var session = await _sessions.RefreshAsync(refreshToken, cancellationToken);
            SetRefreshCookie(session);

            _logger.LogInformation(
                "Refreshed session for user {UserId}.",
                session.Response.User.Id);

            return Ok(session.Response);
        }
        catch (AuthenticationException exception)
        {
            // Do not keep replaying a token that the server has rejected. Return
            // the response here instead of rethrowing: the exception middleware
            // clears response headers, which would discard this Set-Cookie.
            DeleteRefreshCookie();

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication failed",
                Detail = exception.Message,
                Instance = Request.Path,
            };

            if (!string.IsNullOrWhiteSpace(exception.Code))
            {
                problem.Extensions["code"] = exception.Code;
            }

            return Unauthorized(problem);
        }
    }

    /// <summary>Revokes the current refresh token and clears the cookie.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = GetRefreshTokenCookie();

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await _sessions.RevokeAsync(refreshToken, cancellationToken);
        }

        DeleteRefreshCookie();
        return NoContent();
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
    /// Changes the current user's password. All existing refresh tokens are
    /// revoked and a new session is issued.
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

        var currentRefreshToken = GetRefreshTokenCookie();
        var isPersistent = string.IsNullOrWhiteSpace(currentRefreshToken)
            ? true
            : await _sessions.GetPersistenceAsync(currentRefreshToken, cancellationToken) ?? true;

        await _users.ChangePasswordAsync(userId, request, cancellationToken);
        await _sessions.RevokeAllForUserAsync(userId, cancellationToken);

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        var session = await _sessions.CreateAsync(
            user,
            isPersistent,
            cancellationToken);

        SetRefreshCookie(session);

        _logger.LogInformation(
            "User {UserId} changed their password; previous sessions were revoked.",
            userId);

        return Ok(session.Response);
    }

    private string GetRefreshTokenCookie()
    {
        if (!HttpContext.Request.Cookies.TryGetValue(
                RefreshTokenCookieName,
                out var values))
        {
            return string.Empty;
        }

        return values ?? string.Empty;
    }

    private void SetRefreshCookie(AuthSession session)
    {
        Response.Cookies.Append(
            RefreshTokenCookieName,
            session.RefreshToken,
            CreateRefreshCookieOptions(
                session.IsPersistent ? session.RefreshTokenExpiresUtc : null));
    }

    private void DeleteRefreshCookie()
    {
        Response.Cookies.Delete(
            RefreshTokenCookieName,
            CreateRefreshCookieOptions());
    }

    private CookieOptions CreateRefreshCookieOptions(DateTime? expiresUtc = null)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment()
                && !_environment.IsEnvironment("Testing"),
            SameSite = SameSiteMode.Strict,
            Path = RefreshTokenCookiePath,
            IsEssential = true,
        };

        if (expiresUtc.HasValue)
        {
            options.Expires = expiresUtc;
        }

        return options;
    }
}
