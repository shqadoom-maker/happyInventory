using System.Security.Claims;
using happyInventory.DTOs.Auth;
using happyInventory.DTOs.Users;
using happyInventory.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace happyInventory.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ITokenService _tokens;

    public AuthController(IUserService users, ITokenService tokens)
    {
        _users = users;
        _tokens = tokens;
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
}
