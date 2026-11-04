using HappyInventory.Application.Common;
using HappyInventory.Application.DTOs.WarehouseItems;
using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Application.Interfaces.Services;
using HappyInventory.Core.Entities;

namespace HappyInventory.Application.Services;

public class WarehouseItemService : IWarehouseItemService
{
    private const int MaxPageSize = 10;
    private readonly IWarehouseItemRepository _items;

    public WarehouseItemService(IWarehouseItemRepository items)
    {
        _items = items;
    }

    public async Task<PagedResult<WarehouseItemDto>> GetPagedAsync(
        int warehouseId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        if (await _items.GetWarehouseAsync(warehouseId, cancellationToken) is null)
        {
            throw new NotFoundException("The requested warehouse was not found.");
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (items, totalCount) = await _items.GetPagedAsync(
            warehouseId,
            page,
            pageSize,
            search,
            cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<WarehouseItemDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
        };
    }

    public async Task<WarehouseItemDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var item = await _items.GetByIdAsync(id, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException("The requested warehouse item was not found.");
        }

        return ToDto(item);
    }

    public async Task<WarehouseItemDto> CreateAsync(
        WarehouseItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = Require(request.ItemName);
        var warehouse = await GetWarehouseAsync(request.WarehouseId, cancellationToken);

        if (await _items.AnyDuplicateNameAsync(name, warehouse.Id, null, cancellationToken))
        {
            throw new ConflictException(
                $"An item named '{name}' already exists in this warehouse.");
        }

        var item = new WarehouseItem
        {
            ItemName = name,
            SKUCode = NormalizeOptional(request.SKUCode),
            Qty = request.Qty,
            CostPrice = request.CostPrice,
            MSRPPrice = request.MSRPPrice,
            WarehouseId = warehouse.Id,
        };

        await _items.AddAsync(item, cancellationToken);
        await _items.SaveChangesAsync(cancellationToken);

        item.Warehouse = warehouse;

        return ToDto(item);
    }

    public async Task<WarehouseItemDto> UpdateAsync(
        int id,
        WarehouseItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await _items.GetForUpdateAsync(id, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException("The requested warehouse item was not found.");
        }

        var name = Require(request.ItemName);
        var warehouse = await GetWarehouseAsync(request.WarehouseId, cancellationToken);

        if (await _items.AnyDuplicateNameAsync(name, warehouse.Id, id, cancellationToken))
        {
            throw new ConflictException(
                $"An item named '{name}' already exists in this warehouse.");
        }

        item.ItemName = name;
        item.SKUCode = NormalizeOptional(request.SKUCode);
        item.Qty = request.Qty;
        item.CostPrice = request.CostPrice;
        item.MSRPPrice = request.MSRPPrice;
        item.WarehouseId = warehouse.Id;

        await _items.SaveChangesAsync(cancellationToken);

        item.Warehouse = warehouse;

        return ToDto(item);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _items.GetForUpdateAsync(id, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException("The requested warehouse item was not found.");
        }

        _items.Remove(item);
        await _items.SaveChangesAsync(cancellationToken);
    }

    private async Task<Warehouse> GetWarehouseAsync(
        int warehouseId,
        CancellationToken cancellationToken)
    {
        var warehouse = await _items.GetWarehouseAsync(warehouseId, cancellationToken);

        if (warehouse is null)
        {
            throw new ValidationException("The selected warehouse does not exist.");
        }

        return warehouse;
    }

    private static string Require(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ValidationException("Item name is required.");
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static WarehouseItemDto ToDto(WarehouseItem item) => new()
    {
        Id = item.Id,
        ItemName = item.ItemName,
        SKUCode = item.SKUCode,
        Qty = item.Qty,
        CostPrice = item.CostPrice,
        MSRPPrice = item.MSRPPrice,
        WarehouseId = item.WarehouseId,
        WarehouseName = item.Warehouse?.Name ?? string.Empty,
    };
}
