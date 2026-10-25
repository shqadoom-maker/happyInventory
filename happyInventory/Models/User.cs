using happyInventory.Common;

namespace happyInventory.Models;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    /// <summary>Case-insensitive lookup key for <see cref="Username"/>.</summary>
    public string NormalizedUsername { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>A user has exactly one role.</summary>
    public string Role { get; set; } = UserRoles.Auditor;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginUtc { get; set; }
}
