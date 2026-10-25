using HappyInventory.Application.Common;
using HappyInventory.Application.DTOs.Users;
using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Application.Interfaces.Services;
using HappyInventory.Core.Entities;
using HappyInventory.Core.Enums;

namespace HappyInventory.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly string _defaultAdminUsername;

    public UserService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        AdminPolicyOptions adminPolicy)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _defaultAdminUsername = (adminPolicy.DefaultAdminUsername ?? "admin@happywarehouse.com")
            .Trim()
            .ToLowerInvariant();
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _users.GetAllAsync(cancellationToken);
        return users.Select(ToDto).ToList();
    }

    public async Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);

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

        var user = await _users.GetByNormalizedUsernameAsync(normalized, cancellationToken);

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
        await _users.SaveChangesAsync(cancellationToken);

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

        if (await _users.UsernameExistsAsync(normalized, null, cancellationToken))
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

        await _users.AddAsync(user, cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<UserDto> UpdateAsync(
        int id,
        UpdateUserRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);

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

        if (await _users.UsernameExistsAsync(normalized, id, cancellationToken))
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

        await _users.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task ChangePasswordAsync(
        int id,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException("The requested user was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            throw new InvalidOperationException("Password must be at least 8 characters long.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _users.SaveChangesAsync(cancellationToken);
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

        var user = await _users.GetByIdAsync(id, cancellationToken);

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

        _users.Remove(user);
        await _users.SaveChangesAsync(cancellationToken);
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
