using HappyInventory.Application.DTOs.Users;

namespace HappyInventory.Application.Interfaces.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserDto?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, int currentUserId, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(int id, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, int currentUserId, CancellationToken cancellationToken = default);
}
