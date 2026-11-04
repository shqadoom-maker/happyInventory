using System.Security.Cryptography;
using HappyInventory.Application.Interfaces.Services;
using HappyInventory.Core.Entities;
using HappyInventory.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HappyInventory.Infrastructure.Data;

public static class DbSeeder
{
    private const string LegacyUserRole = "User";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DbSeeder));

        await db.Database.MigrateAsync();
        await EnsureDefaultAdminAsync(db, hasher, configuration, logger);
        await ClearLegacyPasswordChangeFlagsAsync(db, logger);
        await NormalizeLegacyRolesAsync(db, logger);
    }

    private static async Task EnsureDefaultAdminAsync(
        AppDbContext db,
        IPasswordHasher hasher,
        IConfiguration configuration,
        ILogger logger)
    {
        var username = configuration["Seed:AdminUsername"]?.Trim();
        var fullName = configuration["Seed:AdminFullName"]?.Trim();

        username = string.IsNullOrWhiteSpace(username) ? "admin@happywarehouse.com" : username;
        fullName = string.IsNullOrWhiteSpace(fullName) ? "System Administrator" : fullName;

        var normalized = username.ToLowerInvariant();

        if (await db.Users.AnyAsync(user => user.NormalizedUsername == normalized))
        {
            // Administrator already exists. Do not reset the password or
            // account details on subsequent startups. Legacy password-change
            // flags are cleared separately below.
            return;
        }

        // Obtain the initial password from configuration. If none is provided,
        // generate a strong initial password. The administrator can change it
        // later, but is not forced to do so at first login.
        var configuredPassword = configuration["Seed:AdminPassword"];
        var password = string.IsNullOrWhiteSpace(configuredPassword)
            ? GenerateInitialPassword()
            : configuredPassword;

        db.Users.Add(CreateDefaultAdmin(username, normalized, fullName, password, hasher));
        await db.SaveChangesAsync();

        if (string.IsNullOrWhiteSpace(configuredPassword))
        {
            logger.LogInformation(
                "Seeded default administrator account '{Username}' with a generated password. " +
                "Change it after signing in.",
                username);
        }
        else
        {
            logger.LogInformation(
                "Seeded default administrator account '{Username}'. Change the password after signing in.",
                username);
        }
    }

    private static string GenerateInitialPassword()
    {
        // 24 bytes base64 = 32 characters, sufficient for an initial password.
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes);
    }

    private static User CreateDefaultAdmin(
        string username,
        string normalized,
        string fullName,
        string password,
        IPasswordHasher hasher) => new()
    {
        Username = username,
        NormalizedUsername = normalized,
        FullName = fullName,
        Role = UserRoles.Admin,
        IsActive = true,
        MustChangePassword = false,
        PasswordHash = hasher.Hash(password),
        CreatedUtc = DateTime.UtcNow,
    };

    private static async Task ClearLegacyPasswordChangeFlagsAsync(AppDbContext db, ILogger logger)
    {
        // Older seeded administrators may still have MustChangePassword = true.
        // Clear the legacy flag without touching their password or other data.
        var users = await db.Users
            .Where(user => user.MustChangePassword)
            .ToListAsync();

        if (users.Count == 0)
        {
            return;
        }

        foreach (var user in users)
        {
            user.MustChangePassword = false;
        }

        await db.SaveChangesAsync();

        logger.LogInformation(
            "Cleared the legacy first-login password-change flag for {Count} account(s).",
            users.Count);
    }

    private static async Task NormalizeLegacyRolesAsync(AppDbContext db, ILogger logger)
    {
        var legacyUsers = await db.Users
            .Where(user => user.Role == LegacyUserRole)
            .ToListAsync();

        if (legacyUsers.Count == 0)
        {
            return;
        }

        foreach (var user in legacyUsers)
        {
            user.Role = UserRoles.Auditor;
        }

        await db.SaveChangesAsync();

        logger.LogInformation(
            "Mapped {Count} legacy 'User' role account(s) to '{Role}'.",
            legacyUsers.Count,
            UserRoles.Auditor);
    }
}
