using HappyInventory.Application.DTOs.Auth;

namespace HappyInventory.Application.Models;

/// <summary>
/// The result of creating or rotating an authenticated session. The refresh
/// token is returned separately from the JSON response so the API layer can put
/// it in an HttpOnly cookie instead of exposing it to JavaScript.
/// </summary>
public sealed record AuthSession(
    LoginResponse Response,
    string RefreshToken,
    DateTime RefreshTokenExpiresUtc,
    bool IsPersistent);
