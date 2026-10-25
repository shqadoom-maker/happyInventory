using happyInventory.Common;
using happyInventory.Data;
using happyInventory.DTOs.Users;
using happyInventory.Models;
using happyInventory.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace happyInventory.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly string _defaultAdminUsername;

    public UserService(AppDbContext db, IPasswordHasher passwordHasher, IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _defaultAdminUsername = (configuration["Seed:AdminUsername"] ?? "admin@happywarehouse.com")
            .Trim()
            .ToLowerInvariant();
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _db.Users
            .AsNoTracking()
            .OrderBy(user => user.Username)
            .ToListAsync(cancellationToken);

        return users.Select(ToDto).ToList();
    }

    public async Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException("The requested user was not found.");
        }

        return ToDto(user);
    }

    public async Task<UserDto?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeUsername(username);

        var user = await _db.Users.FirstOrDefaultAsync(
            candidate => candidate.NormalizedUsername == normalized,
            cancellationToken);

        if (user is null || !_passwordHasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        if (!user.IsActive)
        {
            throw new InvalidOperationException(
                "Your account is disabled and you need to contact support.");
        }

        user.LastLoginUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<UserDto> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = UserRoles.Normalize(request.Role)
            ?? throw new InvalidOperationException(
                $"Role must be one of: {string.Join(", ", UserRoles.All)}.");

        var username = request.Username.Trim();
        var normalized = NormalizeUsername(username);

        var usernameTaken = await _db.Users.AnyAsync(
            candidate => candidate.NormalizedUsername == normalized,
            cancellationToken);

        if (usernameTaken)
        {
            throw new InvalidOperationException($"Username '{username}' is already taken.");
        }

        var user = new User
        {
            Username = username,
            NormalizedUsername = normalized,
            FullName = request.FullName.Trim(),
            Role = role,
            IsActive = request.IsActive,
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedUtc = DateTime.UtcNow,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<UserDto> UpdateAsync(
        int id,
        UpdateUserRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(
            candidate => candidate.Id == id,
            cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException("The requested user was not found.");
        }

        var role = UserRoles.Normalize(request.Role)
            ?? throw new InvalidOperationException(
                $"Role must be one of: {string.Join(", ", UserRoles.All)}.");

        var username = request.Username.Trim();
        var normalized = NormalizeUsername(username);

        if (IsDefaultAdmin(user))
        {
            if (!string.Equals(normalized, _defaultAdminUsername, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The default administrator email cannot be changed.");
            }

            if (role != UserRoles.Admin)
            {
                throw new InvalidOperationException(
                    "The default administrator role cannot be changed.");
            }

            if (!request.IsActive)
            {
                throw new InvalidOperationException(
                    "The default administrator cannot be deactivated.");
            }
        }

        if (id == currentUserId)
        {
            if (!request.IsActive)
            {
                throw new InvalidOperationException("You cannot deactivate your own account.");
            }

            if (role != UserRoles.Admin)
            {
                throw new InvalidOperationException("You cannot remove your own administrator role.");
            }
        }

        var usernameTaken = await _db.Users.AnyAsync(
            candidate => candidate.NormalizedUsername == normalized && candidate.Id != id,
            cancellationToken);

        if (usernameTaken)
        {
            throw new InvalidOperationException($"Username '{username}' is already taken.");
        }

        user.Username = username;
        user.NormalizedUsername = normalized;
        user.FullName = request.FullName.Trim();
        user.Role = role;
        user.IsActive = request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (request.Password.Length < 8)
            {
                throw new InvalidOperationException("Password must be at least 8 characters long.");
            }

            user.PasswordHash = _passwordHasher.Hash(request.Password);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task ChangePasswordAsync(
        int id,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(
            candidate => candidate.Id == id,
            cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException("The requested user was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            throw new InvalidOperationException("Password must be at least 8 characters long.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (id == currentUserId)
        {
            throw new InvalidOperationException("You cannot delete your own account.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(
            candidate => candidate.Id == id,
            cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException("The requested user was not found.");
        }

        if (IsDefaultAdmin(user))
        {
            throw new InvalidOperationException("The default administrator cannot be deleted.");
        }

        if (string.Equals(user.Role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Administrator accounts cannot be deleted.");
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private bool IsDefaultAdmin(User user) =>
        string.Equals(user.NormalizedUsername, _defaultAdminUsername, StringComparison.Ordinal);

    private static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        FullName = user.FullName,
        Role = user.Role,
        IsActive = user.IsActive,
        CreatedUtc = user.CreatedUtc,
        LastLoginUtc = user.LastLoginUtc,
    };
}
