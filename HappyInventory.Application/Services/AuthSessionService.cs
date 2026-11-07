using System.Security.Cryptography;
using System.Text;
using HappyInventory.Application.Common;
using HappyInventory.Application.DTOs.Auth;
using HappyInventory.Application.DTOs.Users;
using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Application.Interfaces.Services;
using HappyInventory.Application.Models;
using HappyInventory.Core.Entities;

namespace HappyInventory.Application.Services;

public sealed class AuthSessionService : IAuthSessionService
{
    private const int RefreshTokenByteLength = 64;

    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenService _tokens;
    private readonly IUserService _users;
    private readonly JwtOptions _jwtOptions;

    public AuthSessionService(
        IRefreshTokenRepository refreshTokens,
        ITokenService tokens,
        IUserService users,
        JwtOptions jwtOptions)
    {
        _refreshTokens = refreshTokens;
        _tokens = tokens;
        _users = users;
        _jwtOptions = jwtOptions;
    }

    public async Task<AuthSession> CreateAsync(
        UserDto user,
        bool isPersistent,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rawRefreshToken = GenerateRefreshToken();
        var refreshToken = CreateRefreshToken(
            user.Id,
            rawRefreshToken,
            isPersistent,
            now);

        await _refreshTokens.AddAsync(refreshToken, cancellationToken);
        await _refreshTokens.SaveChangesAsync(cancellationToken);

        return BuildSession(user, rawRefreshToken, refreshToken);
    }

    public async Task<AuthSession> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new AuthenticationException(
                "Refresh token is missing.",
                "refresh_token_missing");
        }

        var tokenHash = Hash(refreshToken);
        var stored = await _refreshTokens.GetByHashAsync(tokenHash, cancellationToken);

        if (stored is null)
        {
            throw new AuthenticationException(
                "Invalid refresh token.",
                "invalid_refresh_token");
        }

        var now = DateTime.UtcNow;

        if (stored.RevokedUtc is not null)
        {
            
            if (!string.IsNullOrWhiteSpace(stored.ReplacedByTokenHash))
            {
                await RevokeAllForUserAsync(stored.UserId, cancellationToken);
            }

            throw new AuthenticationException(
                "Refresh token has already been used.",
                "refresh_token_reused");
        }

        if (stored.ExpiresUtc <= now)
        {
            throw new AuthenticationException(
                "Refresh token has expired.",
                "refresh_token_expired");
        }

        UserDto user;

        try
        {
            user = await _users.GetByIdAsync(stored.UserId, cancellationToken);
        }
        catch (NotFoundException)
        {
            stored.RevokedUtc = now;
            await _refreshTokens.TrySaveRotationAsync(cancellationToken);

            throw new AuthenticationException(
                "Invalid refresh token.",
                "invalid_refresh_token");
        }

        if (!user.IsActive)
        {
            await RevokeAllForUserAsync(user.Id, cancellationToken);

            throw new AuthenticationException(
                "Your account is disabled and you need to contact support.",
                "account_disabled");
        }

        var rawReplacement = GenerateRefreshToken();
        var replacement = CreateRefreshToken(
            user.Id,
            rawReplacement,
            stored.IsPersistent,
            now);

        stored.RevokedUtc = now;
        stored.ReplacedByTokenHash = replacement.TokenHash;

        await _refreshTokens.AddAsync(replacement, cancellationToken);

        if (!await _refreshTokens.TrySaveRotationAsync(cancellationToken))
        {
            await RevokeAllForUserAsync(stored.UserId, cancellationToken);

            throw new AuthenticationException(
                "Refresh token has already been used.",
                "refresh_token_reused");
        }

        return BuildSession(user, rawReplacement, replacement);
    }

    public async Task<bool?> GetPersistenceAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var stored = await _refreshTokens.GetByHashAsync(
            Hash(refreshToken),
            cancellationToken);

        return stored?.IsPersistent;
    }

    public async Task RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var stored = await _refreshTokens.GetByHashAsync(
            Hash(refreshToken),
            cancellationToken);

        if (stored is null || stored.RevokedUtc is not null)
        {
            return;
        }

        stored.RevokedUtc = DateTime.UtcNow;
        await _refreshTokens.TrySaveRotationAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        await _refreshTokens.RevokeAllForUserAsync(
            userId,
            DateTime.UtcNow,
            cancellationToken);
    }

    private AuthSession BuildSession(
        UserDto user,
        string rawRefreshToken,
        RefreshToken storedToken)
    {
        var (accessToken, accessTokenExpiresUtc) = _tokens.CreateToken(user);

        return new AuthSession(
            new LoginResponse
            {
                Token = accessToken,
                ExpiresUtc = accessTokenExpiresUtc,
                User = user,
            },
            rawRefreshToken,
            storedToken.ExpiresUtc,
            storedToken.IsPersistent);
    }

    private RefreshToken CreateRefreshToken(
        int userId,
        string rawToken,
        bool isPersistent,
        DateTime createdUtc)
    {
        return new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(rawToken),
            CreatedUtc = createdUtc,
            ExpiresUtc = createdUtc.AddDays(_jwtOptions.RefreshTokenExpiryDays),
            IsPersistent = isPersistent,
        };
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(RefreshTokenByteLength);

        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string Hash(string token)
    {
        return Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
