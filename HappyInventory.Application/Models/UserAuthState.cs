namespace HappyInventory.Application.Models;

public sealed record UserAuthState(int Id, bool IsActive, string Role);
