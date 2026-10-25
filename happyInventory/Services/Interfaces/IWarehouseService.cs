using happyInventory.Common;
using happyInventory.DTOs.Warehouses;

namespace happyInventory.Services.Interfaces;

public interface IWarehouseService
{
    Task<PagedResult<WarehouseDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<WarehouseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WarehouseDto> CreateAsync(WarehouseRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseDto> UpdateAsync(int id, WarehouseRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
