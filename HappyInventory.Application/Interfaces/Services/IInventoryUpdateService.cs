using HappyInventory.Application.DTOs.Integrations;

namespace HappyInventory.Application.Interfaces.Services;

/// <summary>
/// Application use case for inbound eCommerce inventory updates.
/// </summary>
public interface IInventoryUpdateService
{
    Task<InventoryUpdateResponse> ApplyAsync(
        InventoryUpdateRequest request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);
}
