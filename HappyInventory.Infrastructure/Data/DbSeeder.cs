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
        var password = configuration["Seed:AdminPassword"];
        var fullName = configuration["Seed:AdminFullName"]?.Trim();

        username = string.IsNullOrWhiteSpace(username) ? "admin@happywarehouse.com" : username;
        password = string.IsNullOrWhiteSpace(password) ? "P@ssw0rd" : password;
        fullName = string.IsNullOrWhiteSpace(fullName) ? "System Administrator" : fullName;

        var normalized = username.ToLowerInvariant();

        if (await db.Users.AnyAsync(user => user.NormalizedUsername == normalized))
        {
            return;
        }

        if (!await db.Users.AnyAsync())
        {
            db.Users.Add(CreateDefaultAdmin(username, normalized, fullName, password, hasher));
            await db.SaveChangesAsync();

            logger.LogInformation(
                "Seeded default administrator account '{Username}'. Change the default password after signing in.",
                username);
            return;
        }

        // Existing database: migrate the previously seeded "admin" account so
        // the documented default credentials keep working without losing the
        // account's history or identity.
        var legacyUsername = configuration["Seed:LegacyAdminUsername"]?.Trim();

        if (!string.IsNullOrWhiteSpace(legacyUsername))
        {
            var legacyNormalized = legacyUsername.ToLowerInvariant();
            var legacyAdmin = await db.Users
                .Where(user => user.NormalizedUsername == legacyNormalized)
                .OrderBy(user => user.Id)
                .FirstOrDefaultAsync();

            if (legacyAdmin is not null &&
                string.Equals(legacyAdmin.Role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                legacyAdmin.Username = username;
                legacyAdmin.NormalizedUsername = normalized;
                legacyAdmin.FullName = string.IsNullOrWhiteSpace(legacyAdmin.FullName)
                    ? fullName
                    : legacyAdmin.FullName;
                legacyAdmin.PasswordHash = hasher.Hash(password);
                legacyAdmin.IsActive = true;

                await db.SaveChangesAsync();

                logger.LogInformation(
                    "Migrated legacy administrator account '{LegacyUsername}' to '{Username}'.",
                    legacyUsername,
                    username);
                return;
            }
        }

        // The default account is missing and there is no legacy account to
        // migrate, so create it. This keeps an administrator available even in
        // databases that were populated before the default account was defined.
        db.Users.Add(CreateDefaultAdmin(username, normalized, fullName, password, hasher));
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Seeded default administrator account '{Username}' in an existing database.",
            username);
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
