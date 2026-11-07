using Bariza.Application.DTOs.Integrations;

namespace Bariza.Application.Interfaces.Services;

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
