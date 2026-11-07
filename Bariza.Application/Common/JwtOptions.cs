using System.Text;

namespace Bariza.Application.Common;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 480;

    /// <summary>
    /// Lifetime of a refresh token. Refresh tokens are rotated on every use, so
    /// this is the maximum idle/session lifetime, not the access-token lifetime.
    /// </summary>
    public int RefreshTokenExpiryDays { get; set; } = 30;

    /// <summary>
    /// Validates the settings that are required to issue and validate JWTs.
    /// Called during startup so a misconfigured deployment fails immediately
    /// instead of issuing tokens with an empty issuer or audience.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Jwt:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(Key))
        {
            throw new InvalidOperationException(
                "Jwt:Key is required. Set it via User Secrets (development) or environment variables (production).");
        }

        if (Encoding.UTF8.GetByteCount(Key) < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes long.");
        }

        if (ExpiryMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt:ExpiryMinutes must be greater than zero.");
        }

        if (RefreshTokenExpiryDays <= 0)
        {
            throw new InvalidOperationException(
                "Jwt:RefreshTokenExpiryDays must be greater than zero.");
        }
    }
}
