namespace HappyInventory.Core.Entities;

/// <summary>
/// A hashed refresh token. The raw token is only ever sent to the browser in an
/// HttpOnly cookie; the database stores a one-way hash so a database leak does
/// not expose usable refresh tokens.
/// </summary>
public class RefreshToken
{
    public long Id { get; set; }

    public int UserId { get; set; }

    /// <summary>SHA-256 hash of the raw refresh token, encoded as Base64.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresUtc { get; set; }

    public DateTime? RevokedUtc { get; set; }

    /// <summary>
    /// Hash of the token that replaced this one during rotation. A non-null
    /// value means the token was already used and a second use is suspicious.
    /// </summary>
    public string? ReplacedByTokenHash { get; set; }

    /// <summary>
    /// Whether the browser should keep the cookie after it closes. This mirrors
    /// the "remember me" choice made at login.
    /// </summary>
    public bool IsPersistent { get; set; } = true;

    public User User { get; set; } = null!;
}
