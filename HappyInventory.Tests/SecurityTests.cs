using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Text;
using HappyInventory.Application.Common;
using HappyInventory.Application.DTOs.Users;
using HappyInventory.Application.Models;
using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Application.Interfaces.Services;
using HappyInventory.Application.Services;
using HappyInventory.Core.Entities;
using HappyInventory.Core.Enums;
using HappyInventory.Infrastructure;
using HappyInventory.Infrastructure.Data;
using HappyInventory.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HappyInventory.Tests;

public class SecurityTests
{
    private static IConfigurationRoot BuildConfiguration(Action<IConfigurationBuilder>? configure = null)
    {
        var builder = new ConfigurationBuilder();
        configure?.Invoke(builder);
        return builder.Build();
    }

    private static AppDbContext CreateMigratedDb()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"happy-test-{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        var db = new AppDbContext(options);
        db.Database.Migrate();
        return db;
    }

    [Fact]
    public void JwtOptions_KeyIsMissing_WhenNotConfigured()
    {
        var config = BuildConfiguration(b => b.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "",
        }));

        var jwtOptions = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>();

        Assert.NotNull(jwtOptions);
        Assert.True(string.IsNullOrWhiteSpace(jwtOptions.Key));
        Assert.True(EncodingUtf8ByteCount(jwtOptions.Key) < 32);
    }

    [Fact]
    public void JwtOptions_KeyIsTooShort_WhenBelow32Bytes()
    {
        var config = BuildConfiguration(b => b.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "too-short",
        }));

        var jwtOptions = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>();

        Assert.NotNull(jwtOptions);
        Assert.True(EncodingUtf8ByteCount(jwtOptions.Key) < 32);
    }

    [Fact]
    public void JwtOptions_Accepts_ValidKey()
    {
        var key = new string('A', 32);
        var config = BuildConfiguration(b => b.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = key,
        }));

        var jwtOptions = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>();

        Assert.Equal(key, jwtOptions.Key);
        Assert.True(EncodingUtf8ByteCount(jwtOptions.Key) >= 32);
    }

    [Fact]
    public void JwtOptions_MissingIssuer_Throws()
    {
        var options = new JwtOptions
        {
            Issuer = "",
            Audience = "happyInventory.Client",
            Key = new string('A', 32),
        };

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Equal("Jwt:Issuer is required.", exception.Message);
    }

    [Fact]
    public void JwtOptions_MissingAudience_Throws()
    {
        var options = new JwtOptions
        {
            Issuer = "happyInventory",
            Audience = "",
            Key = new string('A', 32),
        };

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Equal("Jwt:Audience is required.", exception.Message);
    }

    [Fact]
    public void JwtOptions_ValidConfiguration_DoesNotThrow()
    {
        var options = new JwtOptions
        {
            Issuer = "happyInventory",
            Audience = "happyInventory.Client",
            Key = new string('A', 32),
            ExpiryMinutes = 60,
        };

        options.Validate();
    }

    [Fact]
    public void AddInfrastructure_MissingConnectionString_Throws()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddInfrastructure(configuration));

        Assert.Contains("ConnectionStrings:Default", exception.Message);
    }

    [Fact]
    public void TokenService_EmitsRoleClaim_WithoutMustChangePasswordClaim()
    {
        var options = new JwtOptions
        {
            Key = new string('K', 32),
            Issuer = "happyInventory",
            Audience = "happyInventory.Client",
            ExpiryMinutes = 60,
        };
        var service = new TokenService(options);

        var (token, expiresUtc) = service.CreateToken(new UserDto
        {
            Id = 7,
            Username = "admin@happywarehouse.com",
            FullName = "System Administrator",
            Role = UserRoles.Admin,
            IsActive = true,
        });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.DoesNotContain(jwt.Claims, claim => claim.Type == "mustChangePassword");
        Assert.Contains(
            jwt.Claims,
            claim => claim.Type == ClaimTypes.Role && claim.Value == UserRoles.Admin);
        Assert.Contains(
            jwt.Claims,
            claim => claim.Type == ClaimTypes.Name && claim.Value == "admin@happywarehouse.com");
        Assert.True(expiresUtc > DateTime.UtcNow);
    }

    private static ServiceProvider BuildServiceProvider(AppDbContext db, IPasswordHasher hasher)
    {
        var services = new ServiceCollection();
        services.AddSingleton<AppDbContext>(db);
        services.AddSingleton<IPasswordHasher>(hasher);
        services.AddSingleton<ILoggerFactory>(new TestLoggerFactory());
        services.AddSingleton<IServiceScopeFactory>(new TestScopeFactory(db, hasher));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task DbSeeder_Throws_WhenAdminMissingAndPasswordNotConfigured()
    {
        var db = CreateMigratedDb();
        var hasher = new PasswordHasher();
        var config = BuildConfiguration(b => b.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Seed:AdminUsername"] = "admin@happywarehouse.com",
            // No Seed:AdminPassword -> creating an unrecoverable account is unsafe
        }));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DbSeeder.SeedAsync(BuildServiceProvider(db, hasher), config));

        Assert.Contains("Seed:AdminPassword", exception.Message);
        Assert.Empty(await db.Users.ToListAsync());
    }

    [Fact]
    public async Task DbSeeder_DoesNotRequirePassword_WhenAdminAlreadyExists()
    {
        var db = CreateMigratedDb();
        var hasher = new PasswordHasher();
        var firstConfig = BuildConfiguration(b => b.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Seed:AdminUsername"] = "admin@happywarehouse.com",
            ["Seed:AdminPassword"] = "InitialPassword123!",
        }));

        await DbSeeder.SeedAsync(BuildServiceProvider(db, hasher), firstConfig);

        var secondConfig = BuildConfiguration(b => b.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Seed:AdminUsername"] = "admin@happywarehouse.com",
            // Existing administrator: no password is needed on later startups.
        }));

        await DbSeeder.SeedAsync(BuildServiceProvider(db, hasher), secondConfig);

        var user = await db.Users.SingleAsync(u => u.NormalizedUsername == "admin@happywarehouse.com");
        Assert.True(hasher.Verify("InitialPassword123!", user.PasswordHash));
    }

    [Fact]
    public async Task DbSeeder_CreatesAdmin_WithConfiguredPassword_WhenProvided()
    {
        var db = CreateMigratedDb();
        var hasher = new PasswordHasher();
        var config = BuildConfiguration(b => b.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Seed:AdminUsername"] = "admin@happywarehouse.com",
            ["Seed:AdminPassword"] = "SecurePassword123!",
        }));

        await DbSeeder.SeedAsync(BuildServiceProvider(db, hasher), config);

        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == "admin@happywarehouse.com");

        Assert.NotNull(user);
        Assert.False(user.MustChangePassword);
        Assert.True(hasher.Verify("SecurePassword123!", user.PasswordHash));
    }

    [Fact]
    public async Task DbSeeder_ClearsLegacyPasswordChangeFlag_WithoutResettingPassword()
    {
        var db = CreateMigratedDb();
        var hasher = new PasswordHasher();
        var config = BuildConfiguration(b => b.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Seed:AdminUsername"] = "admin@happywarehouse.com",
            ["Seed:AdminPassword"] = "InitialPassword123!",
        }));

        await DbSeeder.SeedAsync(BuildServiceProvider(db, hasher), config);

        var user = await db.Users.FirstAsync(u => u.NormalizedUsername == "admin@happywarehouse.com");
        user.MustChangePassword = true;
        user.PasswordHash = hasher.Hash("ChangedPassword123!");
        await db.SaveChangesAsync();

        await DbSeeder.SeedAsync(BuildServiceProvider(db, hasher), config);

        var updated = await db.Users.FirstAsync(u => u.NormalizedUsername == "admin@happywarehouse.com");
        Assert.False(updated.MustChangePassword);
        Assert.True(hasher.Verify("ChangedPassword123!", updated.PasswordHash));
    }

    [Fact]
    public async Task DbSeeder_DoesNotResetPassword_OnSubsequentStartups()
    {
        var db = CreateMigratedDb();
        var hasher = new PasswordHasher();
        var config = BuildConfiguration(b => b.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Seed:AdminUsername"] = "admin@happywarehouse.com",
            ["Seed:AdminPassword"] = "FirstPassword123!",
        }));

        // First startup: create admin
        await DbSeeder.SeedAsync(BuildServiceProvider(db, hasher), config);
        var firstHash = (await db.Users.FirstAsync(u => u.NormalizedUsername == "admin@happywarehouse.com")).PasswordHash;

        // Change the password externally
        var user = await db.Users.FirstAsync(u => u.NormalizedUsername == "admin@happywarehouse.com");
        user.PasswordHash = hasher.Hash("ChangedPassword123!");
        await db.SaveChangesAsync();

        // Second startup with a different configured password: should NOT reset
        var config2 = BuildConfiguration(b => b.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Seed:AdminUsername"] = "admin@happywarehouse.com",
            ["Seed:AdminPassword"] = "DifferentPassword123!",
        }));

        await DbSeeder.SeedAsync(BuildServiceProvider(db, hasher), config2);

        var unchangedUser = await db.Users.FirstAsync(u => u.NormalizedUsername == "admin@happywarehouse.com");
        Assert.NotEqual(firstHash, unchangedUser.PasswordHash);
        Assert.True(hasher.Verify("ChangedPassword123!", unchangedUser.PasswordHash));
    }

    [Fact]
    public async Task UserService_ChangePassword_UpdatesPassword()
    {
        var db = CreateMigratedDb();
        var hasher = new PasswordHasher();
        var repo = new TestUserRepository(db);
        var service = new UserService(repo, hasher, new AdminPolicyOptions { DefaultAdminUsername = "admin@happywarehouse.com" });

        var user = new User
        {
            Username = "admin@happywarehouse.com",
            NormalizedUsername = "admin@happywarehouse.com",
            FullName = "Admin",
            Role = UserRoles.Admin,
            IsActive = true,
            MustChangePassword = true,
            PasswordHash = hasher.Hash("OldPassword123!"),
            CreatedUtc = DateTime.UtcNow,
        };
        await repo.AddAsync(user);
        await repo.SaveChangesAsync();

        await service.ChangePasswordAsync(
            user.Id,
            new ChangePasswordRequest
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!",
            },
            default);

        var updated = await repo.GetByIdAsync(user.Id, default);
        Assert.NotNull(updated);
        Assert.False(updated.MustChangePassword);
        Assert.True(hasher.Verify("NewPassword123!", updated.PasswordHash));
    }

    [Fact]
    public async Task UserService_ChangePassword_RejectsWrongCurrentPassword()
    {
        var db = CreateMigratedDb();
        var hasher = new PasswordHasher();
        var repo = new TestUserRepository(db);
        var service = new UserService(repo, hasher, new AdminPolicyOptions { DefaultAdminUsername = "admin@happywarehouse.com" });

        var user = new User
        {
            Username = "user@test.com",
            NormalizedUsername = "user@test.com",
            FullName = "Test User",
            Role = UserRoles.Auditor,
            IsActive = true,
            PasswordHash = hasher.Hash("OldPassword123!"),
            CreatedUtc = DateTime.UtcNow,
        };
        await repo.AddAsync(user);
        await repo.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.ChangePasswordAsync(
                user.Id,
                new ChangePasswordRequest
                {
                    CurrentPassword = "WrongPassword123!",
                    NewPassword = "NewPassword123!",
                },
                default));

        Assert.Equal("Current password is incorrect.", exception.Message);

        var updated = await repo.GetByIdAsync(user.Id, default);
        Assert.NotNull(updated);
        Assert.True(hasher.Verify("OldPassword123!", updated.PasswordHash));
    }

    [Fact]
    public async Task UserService_ResetPassword_DoesNotRequireCurrentPassword()
    {
        var db = CreateMigratedDb();
        var hasher = new PasswordHasher();
        var repo = new TestUserRepository(db);
        var service = new UserService(repo, hasher, new AdminPolicyOptions { DefaultAdminUsername = "admin@happywarehouse.com" });

        var user = new User
        {
            Username = "user@test.com",
            NormalizedUsername = "user@test.com",
            FullName = "Test User",
            Role = UserRoles.Auditor,
            IsActive = true,
            MustChangePassword = true,
            PasswordHash = hasher.Hash("OldPassword123!"),
            CreatedUtc = DateTime.UtcNow,
        };
        await repo.AddAsync(user);
        await repo.SaveChangesAsync();

        await service.ResetPasswordAsync(
            user.Id,
            new ResetPasswordRequest { NewPassword = "ResetPassword123!" },
            default);

        var updated = await repo.GetByIdAsync(user.Id, default);
        Assert.NotNull(updated);
        Assert.False(updated.MustChangePassword);
        Assert.True(hasher.Verify("ResetPassword123!", updated.PasswordHash));
    }

    [Fact]
    public async Task UserService_Authenticate_DoesNotChangePasswordHash()
    {
        var db = CreateMigratedDb();
        var hasher = new PasswordHasher();
        var repo = new TestUserRepository(db);
        var service = new UserService(repo, hasher, new AdminPolicyOptions { DefaultAdminUsername = "admin@happywarehouse.com" });

        var user = new User
        {
            Username = "user@test.com",
            NormalizedUsername = "user@test.com",
            FullName = "Test User",
            Role = UserRoles.Auditor,
            IsActive = true,
            PasswordHash = hasher.Hash("Password123!"),
            CreatedUtc = DateTime.UtcNow,
        };
        await repo.AddAsync(user);
        await repo.SaveChangesAsync();

        var result = await service.AuthenticateAsync("user@test.com", "Password123!", default);

        Assert.NotNull(result);
        Assert.True(hasher.Verify("Password123!", user.PasswordHash));
    }

    [Fact]
    public async Task UserService_Authenticate_ReturnsNull_WhenUserDoesNotExist()
    {
        var db = CreateMigratedDb();
        var hasher = new PasswordHasher();
        var repo = new TestUserRepository(db);
        var service = new UserService(repo, hasher, new AdminPolicyOptions { DefaultAdminUsername = "admin@happywarehouse.com" });

        var result = await service.AuthenticateAsync("nonexistent@test.com", "AnyPassword123!", default);

        Assert.Null(result);
    }

    [Fact]
    public async Task UserService_Authenticate_ReturnsNull_WhenPasswordIsWrong()
    {
        var db = CreateMigratedDb();
        var hasher = new PasswordHasher();
        var repo = new TestUserRepository(db);
        var service = new UserService(repo, hasher, new AdminPolicyOptions { DefaultAdminUsername = "admin@happywarehouse.com" });

        var user = new User
        {
            Username = "user@test.com",
            NormalizedUsername = "user@test.com",
            FullName = "Test User",
            Role = UserRoles.Auditor,
            IsActive = true,
            PasswordHash = hasher.Hash("CorrectPassword123!"),
            CreatedUtc = DateTime.UtcNow,
        };
        await repo.AddAsync(user);
        await repo.SaveChangesAsync();

        var result = await service.AuthenticateAsync("user@test.com", "WrongPassword123!", default);

        Assert.Null(result);
    }

    [Fact]
    public async Task UserService_Authenticate_Succeeds_WithCorrectCredentials()
    {
        var db = CreateMigratedDb();
        var hasher = new PasswordHasher();
        var repo = new TestUserRepository(db);
        var service = new UserService(repo, hasher, new AdminPolicyOptions { DefaultAdminUsername = "admin@happywarehouse.com" });

        var user = new User
        {
            Username = "user@test.com",
            NormalizedUsername = "user@test.com",
            FullName = "Test User",
            Role = UserRoles.Auditor,
            IsActive = true,
            PasswordHash = hasher.Hash("CorrectPassword123!"),
            CreatedUtc = DateTime.UtcNow,
        };
        await repo.AddAsync(user);
        await repo.SaveChangesAsync();

        var result = await service.AuthenticateAsync("user@test.com", "CorrectPassword123!", default);

        Assert.NotNull(result);
        Assert.Equal("user@test.com", result.Username);
        Assert.Equal(UserRoles.Auditor, result.Role);
    }

    private static int EncodingUtf8ByteCount(string? value) =>
        value is null ? 0 : Encoding.UTF8.GetByteCount(value);

    private sealed class TestScopeFactory : IServiceScopeFactory
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher _hasher;

        public TestScopeFactory(AppDbContext db, IPasswordHasher hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        public IServiceScope CreateScope() => new TestScope(_db, _hasher);

        private sealed class TestScope : IServiceScope
        {
            private readonly AppDbContext _db;
            private readonly IPasswordHasher _hasher;
            private readonly ServiceProvider _provider;

            public TestScope(AppDbContext db, IPasswordHasher hasher)
            {
                _db = db;
                _hasher = hasher;
                var services = new ServiceCollection();
                services.AddSingleton<AppDbContext>(db);
                services.AddSingleton<IPasswordHasher>(hasher);
                services.AddSingleton<ILoggerFactory>(new TestLoggerFactory());
                _provider = services.BuildServiceProvider();
            }

            public IServiceProvider ServiceProvider => _provider;

            public void Dispose() { }
        }
    }

    private sealed class TestLoggerFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider) { }
        public ILogger CreateLogger(string categoryName) => new TestLogger();
        public void Dispose() { }
    }

    private sealed class TestLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) => null!;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    private sealed class TestUserRepository : IUserRepository
    {
        private readonly AppDbContext _db;

        public TestUserRepository(AppDbContext db)
        {
            _db = db;
        }

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
            _db.Users.ToListAsync(cancellationToken).ContinueWith(t => (IReadOnlyList<User>)t.Result, cancellationToken);

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        public Task<User?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default) =>
            _db.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername, cancellationToken);

        public Task<UserAuthState?> GetAuthStateAsync(int id, CancellationToken cancellationToken = default) =>
            _db.Users.AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new UserAuthState(u.Id, u.IsActive, u.Role))
                .FirstOrDefaultAsync(cancellationToken);

        public Task<bool> UsernameExistsAsync(string normalizedUsername, int? excludedId, CancellationToken cancellationToken = default) =>
            _db.Users.AnyAsync(u => u.NormalizedUsername == normalizedUsername && (excludedId == null || u.Id != excludedId), cancellationToken);

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _db.Users.Add(user);
            return Task.CompletedTask;
        }

        public void Remove(User user) => _db.Users.Remove(user);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _db.SaveChangesAsync(cancellationToken);
    }
}
