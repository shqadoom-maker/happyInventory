using happyInventory.Common;
using happyInventory.Data;
using happyInventory.DTOs.WarehouseItems;
using happyInventory.Models;
using happyInventory.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace happyInventory.Services;

public class WarehouseItemService : IWarehouseItemService
{
    private const int MaxPageSize = 10;
    private readonly AppDbContext _db;

    public WarehouseItemService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<WarehouseItemDto>> GetPagedAsync(
        int warehouseId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        await EnsureWarehouseExistsAsync(warehouseId, cancellationToken);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _db.WarehouseItems
            .AsNoTracking()
            .Include(item => item.Warehouse)
            .Where(item => item.WarehouseId == warehouseId)
            .AsQueryable();

        var term = search?.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(item =>
                item.ItemName.ToLower().Contains(term) ||
                (item.SKUCode != null && item.SKUCode.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderBy(item => item.ItemName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

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
        var item = await _db.WarehouseItems
            .AsNoTracking()
            .Include(candidate => candidate.Warehouse)
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (item is null)
        {
            throw new KeyNotFoundException("The requested warehouse item was not found.");
        }

        return ToDto(item);
    }

    public async Task<WarehouseItemDto> CreateAsync(
        WarehouseItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = Require(request.ItemName);
        var warehouse = await GetWarehouseAsync(request.WarehouseId, cancellationToken);

        await EnsureUniqueNameAsync(name, warehouse.Id, null, cancellationToken);

        var item = new WarehouseItem
        {
            ItemName = name,
            SKUCode = NormalizeOptional(request.SKUCode),
            Qty = request.Qty,
            CostPrice = request.CostPrice,
            MSRPPrice = request.MSRPPrice,
            WarehouseId = warehouse.Id,
        };

        _db.WarehouseItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        item.Warehouse = warehouse;

        return ToDto(item);
    }

    public async Task<WarehouseItemDto> UpdateAsync(
        int id,
        WarehouseItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await _db.WarehouseItems
            .Include(candidate => candidate.Warehouse)
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (item is null)
        {
            throw new KeyNotFoundException("The requested warehouse item was not found.");
        }

        var name = Require(request.ItemName);
        var warehouse = await GetWarehouseAsync(request.WarehouseId, cancellationToken);

        await EnsureUniqueNameAsync(name, warehouse.Id, id, cancellationToken);

        item.ItemName = name;
        item.SKUCode = NormalizeOptional(request.SKUCode);
        item.Qty = request.Qty;
        item.CostPrice = request.CostPrice;
        item.MSRPPrice = request.MSRPPrice;
        item.WarehouseId = warehouse.Id;

        await _db.SaveChangesAsync(cancellationToken);

        item.Warehouse = warehouse;

        return ToDto(item);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _db.WarehouseItems.FirstOrDefaultAsync(
            candidate => candidate.Id == id,
            cancellationToken);

        if (item is null)
        {
            throw new KeyNotFoundException("The requested warehouse item was not found.");
        }

        _db.WarehouseItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureWarehouseExistsAsync(
        int warehouseId,
        CancellationToken cancellationToken)
    {
        var exists = await _db.Warehouses.AnyAsync(
            warehouse => warehouse.Id == warehouseId,
            cancellationToken);

        if (!exists)
        {
            throw new KeyNotFoundException("The requested warehouse was not found.");
        }
    }

    private async Task<Warehouse> GetWarehouseAsync(
        int warehouseId,
        CancellationToken cancellationToken)
    {
        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(
            candidate => candidate.Id == warehouseId,
            cancellationToken);

        if (warehouse is null)
        {
            throw new InvalidOperationException("The selected warehouse does not exist.");
        }

        return warehouse;
    }

    private async Task EnsureUniqueNameAsync(
        string name,
        int warehouseId,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        var normalized = name.ToLowerInvariant();

        var duplicate = await _db.WarehouseItems.AnyAsync(
            item =>
                (excludedId == null || item.Id != excludedId) &&
                item.WarehouseId == warehouseId &&
                item.ItemName.ToLower() == normalized,
            cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                $"An item named '{name}' already exists in this warehouse.");
        }
    }

    private static string Require(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new InvalidOperationException("Item name is required.");
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
