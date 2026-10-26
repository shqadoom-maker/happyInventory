using System.IO;
using System.Text;
using HappyInventory.Application.Common;
using HappyInventory.Application.DTOs.Users;
using HappyInventory.Application.Models;
using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Application.Interfaces.Services;
using HappyInventory.Application.Services;
using HappyInventory.Core.Entities;
using HappyInventory.Core.Enums;
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
    public async Task DbSeeder_CreatesAdmin_WithMustChangePassword_WhenNoPasswordConfigured()
    {
        var db = CreateMigratedDb();
        var hasher = new PasswordHasher();
        var config = BuildConfiguration(b => b.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Seed:AdminUsername"] = "admin@happywarehouse.com",
            // No Seed:AdminPassword -> one-time password should be generated
        }));

        await DbSeeder.SeedAsync(BuildServiceProvider(db, hasher), config);

        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == "admin@happywarehouse.com");

        Assert.NotNull(user);
        Assert.Equal(UserRoles.Admin, user.Role);
        Assert.True(user.IsActive);
        Assert.True(user.MustChangePassword);
        Assert.NotEmpty(user.PasswordHash);
        Assert.NotEqual("P@ssw0rd", user.PasswordHash);
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
        Assert.True(user.MustChangePassword);
        Assert.True(hasher.Verify("SecurePassword123!", user.PasswordHash));
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
    public async Task UserService_ChangePassword_ClearsMustChangePassword()
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

        await service.ChangePasswordAsync(user.Id, new ChangePasswordRequest { NewPassword = "NewPassword123!" }, default);

        var updated = await repo.GetByIdAsync(user.Id, default);
        Assert.NotNull(updated);
        Assert.False(updated.MustChangePassword);
        Assert.True(hasher.Verify("NewPassword123!", updated.PasswordHash));
    }

    [Fact]
    public async Task UserService_Authenticate_DoesNotClearMustChangePassword()
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
            PasswordHash = hasher.Hash("Password123!"),
            CreatedUtc = DateTime.UtcNow,
        };
        await repo.AddAsync(user);
        await repo.SaveChangesAsync();

        var result = await service.AuthenticateAsync("user@test.com", "Password123!", default);

        Assert.NotNull(result);
        Assert.True(result.MustChangePassword);
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
