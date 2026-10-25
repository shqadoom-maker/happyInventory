using happyInventory.Common;
using happyInventory.DTOs.WarehouseItems;

namespace happyInventory.Services.Interfaces;

public interface IWarehouseItemService
{
    Task<PagedResult<WarehouseItemDto>> GetPagedAsync(
        int warehouseId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);

    Task<WarehouseItemDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WarehouseItemDto> CreateAsync(
        WarehouseItemRequest request,
        CancellationToken cancellationToken = default);

    Task<WarehouseItemDto> UpdateAsync(
        int id,
        WarehouseItemRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
