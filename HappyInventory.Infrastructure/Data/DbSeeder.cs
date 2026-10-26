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
            // Administrator already exists. Do not reset the password or any
            // other property on subsequent startups.
            return;
        }

        // Obtain the initial password from configuration. If none is provided,
        // generate a one-time password that must be changed at first login.
        var configuredPassword = configuration["Seed:AdminPassword"];
        var password = string.IsNullOrWhiteSpace(configuredPassword)
            ? GenerateOneTimePassword()
            : configuredPassword;

        db.Users.Add(CreateDefaultAdmin(username, normalized, fullName, password, hasher));
        await db.SaveChangesAsync();

        if (string.IsNullOrWhiteSpace(configuredPassword))
        {
            // Only log the one-time password in development. Never log it in
            // production; retrieve it from the secure secret store instead.
            logger.LogInformation(
                "Seeded default administrator account '{Username}' with a one-time password. " +
                "The administrator must change it at first login.",
                username);
        }
        else
        {
            logger.LogInformation(
                "Seeded default administrator account '{Username}'. Change the password after signing in.",
                username);
        }
    }

    private static string GenerateOneTimePassword()
    {
        // 24 bytes base64-url = 32 characters, sufficient for a one-time password.
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
        // Force the administrator to change the one-time password at first login.
        MustChangePassword = true,
        PasswordHash = hasher.Hash(password),
        CreatedUtc = DateTime.UtcNow,
    };

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
